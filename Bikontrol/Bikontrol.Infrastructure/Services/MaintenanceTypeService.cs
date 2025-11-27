using AutoMapper;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;

public class MaintenanceTypeService : IMaintenanceTypeService
{
    private readonly IMaintenanceTypeRepository _repo;
    private readonly IUserMaintenanceTypeRepository _userRepo;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _current;

    public MaintenanceTypeService(
        IMaintenanceTypeRepository repo,
        IUserMaintenanceTypeRepository userRepo,
        IMapper mapper,
        ICurrentUserService current)
    {
        _repo = repo;
        _userRepo = userRepo;
        _mapper = mapper;
        _current = current;
    }

    public async Task<IEnumerable<MaintenanceTypeDTO>> GetDefaultsAsync()
    {
        var list = await _repo.GetAllAsync();
        return _mapper.Map<IEnumerable<MaintenanceTypeDTO>>(list);
    }

    public async Task<IEnumerable<MaintenanceTypeDTO>> GetUserTypesAsync()
    {
        var list = await _userRepo.GetByUserIdAsync(_current.UserId);
        return _mapper.Map<IEnumerable<MaintenanceTypeDTO>>(list);
    }

    public async Task<MaintenanceTypeDTO> CreateUserTypeAsync(CreateMaintenanceTypeDTO dto)
    {
        var entity = _mapper.Map<UserMaintenanceType>(dto);
        entity.UserId = _current.UserId;

        var created = await _userRepo.AddAsync(entity);
        return _mapper.Map<MaintenanceTypeDTO>(created);
    }

    public async Task DisableUserTypeAsync(Guid id)
    {
        await _userRepo.SoftDeleteAsync(id);
    }

    public async Task DeleteUserTypeAsync(Guid id)
    {
        await _userRepo.SoftDeleteAsync(id);
    }
}
