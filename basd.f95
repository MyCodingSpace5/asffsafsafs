module VoxelEngine
    implicit none
    
    ! Define our block types as constant integers
    integer, parameter :: BLOCK_AIR   = 0
    integer, parameter :: BLOCK_STONE = 1
    integer, parameter :: BLOCK_DIRT  = 2
    integer, parameter :: BLOCK_GRASS = 3

    ! Chunk dimensions
    integer, parameter :: CHUNK_SIZE   = 16
    integer, parameter :: CHUNK_HEIGHT = 256

contains

    ! Mock function for 3D Noise. 
    ! In Fortran, parameters are passed by reference by default, 
    ! so we mark them as 'intent(in)' for optimization.
    real function get_3d_noise(x, y, z)
        real, intent(in) :: x, y, z
        get_3d_noise = 0.0  ! Placeholder for Perlin/Simplex noise
    end function get_3d_noise

    ! The core chunk generation subroutine
    subroutine generate_chunk(chunk_offset_x, chunk_offset_z, chunk_data)
        integer, intent(in) :: chunk_offset_x, chunk_offset_z
        ! A 3D array allocated outside and passed in
        integer, intent(out) :: chunk_data(CHUNK_SIZE, CHUNK_HEIGHT, CHUNK_SIZE)
        
        integer :: x, y, z
        real :: global_x, global_z
        real :: noise_scale, raw_density, height_modifier, final_density
        integer :: depth_below_surface

        noise_scale = 0.05

        ! CRITICAL FORTRAN OPTIMIZATION: 
        ! We loop with Z on the outside and X on the inside. 
        ! Because Fortran arrays are column-major, chunk_data(x, y, z) 
        ! updates contiguous memory slots when 'x' changes fastest.
        do z = 1, CHUNK_SIZE
            global_z = (chunk_offset_z * CHUNK_SIZE) + (z - 1)
            
            do x = 1, CHUNK_SIZE
                global_x = (chunk_offset_x * CHUNK_SIZE) + (x - 1)
                
                depth_below_surface = 0

                ! Loop top-down through the Y axis (Height)
                do y = CHUNK_HEIGHT, 1, -1
                    
                    ! Sample the noise
                    raw_density = get_3d_noise(global_x * noise_scale, &
                                               real(y) * noise_scale, &
                                               global_z * noise_scale)

                    ! Apply height falloff math
                    height_modifier = (128.0 - real(y)) / 64.0
                    final_density = raw_density + height_modifier

                    ! Block placement logic
                    if (final_density > 0.0) then
                        if (depth_below_surface == 0) then
                            chunk_data(x, y, z) = BLOCK_GRASS
                        else if (depth_below_surface < 4) then
                            chunk_data(x, y, z) = BLOCK_DIRT
                        else
                            chunk_data(x, y, z) = BLOCK_STONE
                        endif
                        depth_below_surface = depth_below_surface + 1
                    else
                        chunk_data(x, y, z) = BLOCK_AIR
                        depth_below_surface = 0
                    endif

                end do
            end do
        end do

    end subroutine generate_chunk

end module VoxelEngine


program main
    use VoxelEngine
    implicit none

    ! Allocate the chunk array (Fortran arrays are 1-indexed by default)
    integer :: my_chunk(CHUNK_SIZE, CHUNK_HEIGHT, CHUNK_SIZE)

    ! Generate chunk at grid coordinates (0, 0)
    call generate_chunk(0, 0, my_chunk)

    print *, "Fortran chunk generated successfully!"
end program main
