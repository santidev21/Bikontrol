using AutoMapper;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
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

        public MotorcycleService(IMotorcycleRepository motorcycleRepository, IMapper mapper)
        {
            _motorcycleRepository = motorcycleRepository;
            _mapper = mapper;
        }

        public async Task<MotorcycleDTO> CreateAsync(CreateMotorcycleDTO dto)
        {
            var entity = _mapper.Map<Motorcycle>(dto);
            var created = await _motorcycleRepository.AddAsync(entity);
            return _mapper.Map<MotorcycleDTO>(created);
        }

        public async Task<MotorcycleDTO?> GetByIdAsync(Guid id)
        {
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            return entity is null ? null : _mapper.Map<MotorcycleDTO>(entity);
        }

        public async Task<IList<MotorcycleDTO>> GetByUserIdAsync(Guid userId)
        {
            var motorcycles = await _motorcycleRepository.GetByUserIdAsync(userId);
            return _mapper.Map<IList<MotorcycleDTO>>(motorcycles);
        }

        public async Task UpdateAsync(Guid id, UpdateMotorcycleDTO dto)
        {
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new KeyNotFoundException("Motocicleta no encontrada.");

            _mapper.Map(dto, entity);
            await _motorcycleRepository.UpdateAsync(entity);
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            await _motorcycleRepository.SoftDeleteAsync(id);
        }
    }
}
