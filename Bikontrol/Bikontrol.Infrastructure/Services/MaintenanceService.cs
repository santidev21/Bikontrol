using AutoMapper;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;

public class MaintenanceService : IMaintenanceService
{
    private readonly IMaintenanceRepository _repo;
    private readonly IUserMaintenanceRepository _userRepo;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _current;

    public MaintenanceService(
        IMaintenanceRepository repo,
        IUserMaintenanceRepository userRepo,
        IMapper mapper,
        ICurrentUserService current)
    {
        _repo = repo;
        _userRepo = userRepo;
        _mapper = mapper;
        _current = current;
    }

    public async Task<IEnumerable<MaintenanceDTO>> GetDefaultsAsync()
    {
        var list = await _repo.GetAllAsync();
        return _mapper.Map<IEnumerable<MaintenanceDTO>>(list);
    }

    public async Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceAsync()
    {
        var list = await _userRepo.GetByUserIdAsync(_current.UserId);
        return _mapper.Map<IEnumerable<MaintenanceDTO>>(list);
    }

    public async Task<MaintenanceDTO> CreateUserMaintenanceAsync(SaveMaintenanceDTO dto)
    {
        var entity = _mapper.Map<UserMaintenance>(dto);
        entity.UserId = _current.UserId;

        var created = await _userRepo.AddAsync(entity);
        return _mapper.Map<MaintenanceDTO>(created);
    }

    public async Task DeleteUserMaintenanceAsync(Guid id)
    {
        await _userRepo.SoftDeleteAsync(id);
    }
}
