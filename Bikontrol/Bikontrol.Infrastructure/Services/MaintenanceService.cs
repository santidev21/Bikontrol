using AutoMapper;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Persistence.Repositories;
using Bikontrol.Shared.Exceptions;

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
        var list = await _repo.GetAllForUserAsync(_current.UserId);
        return _mapper.Map<IEnumerable<MaintenanceDTO>>(list);
    }

    public async Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceAsync()
    {
        var list = await _userRepo.GetByUserIdAsync(_current.UserId);
        return _mapper.Map<IEnumerable<MaintenanceDTO>>(list);
    }

    public async Task<MaintenanceDTO?> GetByIdAsync(Guid id)
    {
        var maintenance = await _userRepo.GetByIdAsync(id);
        return _mapper.Map<MaintenanceDTO>(maintenance);
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
    
    public async Task<MaintenanceDTO> FollowDefaultAsync(Guid defaultId, int? KmInterval, int? TimeIntervalWeeks, string TrackingType)
    {
        var defaultEntity = await _repo.GetByIdAsync(defaultId);
        if (defaultEntity == null)
            throw new NotFoundException("Default maintenance type not found.");

        // Check if already followed
        var existing = await _userRepo.GetByBaseIdAsync(
            _current.UserId, defaultId
        );

        if (existing != null)
        {
            if(existing.IsEnabled) throw new ValidationException("You are already following this maintenance.");
            else
            {
                existing.IsEnabled = true;
                await _userRepo.UpdateAsync(existing);
                return _mapper.Map<MaintenanceDTO>(existing);
            }
        }

        var entity = new UserMaintenance
        {
            UserId = _current.UserId,
            BaseTypeId = defaultEntity.Id,
            Name = defaultEntity.Name,
            Description = defaultEntity.Description,
            KmInterval = KmInterval,
            TimeIntervalWeeks = TimeIntervalWeeks,
            TrackingType = TrackingType,
            IsEnabled = true
        };

        var created = await _userRepo.AddAsync(entity);
        return _mapper.Map<MaintenanceDTO>(created);
    }
    public async Task UpdateAsync(Guid id, SaveMaintenanceDTO dto)
    {
        var entity = await _userRepo.GetByIdAsync(id);
        if (entity is null) throw new NotFoundException("Mantenimiento no encontrado.");

        if (entity.UserId != _current.UserId)
            throw new ForbiddenAccessException("No tienes permisos para editar este mantenimiento.");

        _mapper.Map(dto, entity);
        await _userRepo.UpdateAsync(entity);
    }
}
