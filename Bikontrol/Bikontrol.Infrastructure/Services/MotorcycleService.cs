using AutoMapper;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Shared.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Infrastructure.Services
{
    public class MotorcycleService : IMotorcycleService
    {
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public MotorcycleService(IMotorcycleRepository motorcycleRepository, IMapper mapper, ICurrentUserService currentUser)
        {
            _motorcycleRepository = motorcycleRepository;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<MotorcycleDTO> CreateAsync(CreateMotorcycleDTO dto)
        {
            var entity = _mapper.Map<Motorcycle>(dto);
            entity.UserId = _currentUser.UserId;
            entity.Validate();
            var created = await _motorcycleRepository.AddAsync(entity);
            return _mapper.Map<MotorcycleDTO>(created);
        }

        public async Task<MotorcycleDTO?> GetByIdAsync(Guid id)
        {
            var entity = await _motorcycleRepository.GetByIdAsync(id);

            if (entity == null || entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes acceso a esta motocicleta.");

            return _mapper.Map<MotorcycleDTO>(entity);
        }

        public async Task<IList<MotorcycleDTO>> GetByCurrentUserAsync()
        {
            var motorcycles = await _motorcycleRepository.GetByUserIdAsync(_currentUser.UserId);
            return _mapper.Map<IList<MotorcycleDTO>>(motorcycles);
        }

        public async Task UpdateAsync(Guid id, UpdateMotorcycleDTO dto)
        {
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Motocicleta no encontrada.");

            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes permisos para editar esta motocicleta.");

            _mapper.Map(dto, entity);
            await _motorcycleRepository.UpdateAsync(entity);
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Motocicleta no encontrada.");

            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes permisos para borrar esta motocicleta.");

            await _motorcycleRepository.SoftDeleteAsync(id);
        }
    }
}
