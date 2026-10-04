using AutoMapper;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Services;
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
        private readonly IKmHistoryRepository _kmHistoryRepository;
        private readonly IKmHistoryService _kmHistoryService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;
        private readonly ITransactionManager _transactions;

        public MotorcycleService(IMotorcycleRepository motorcycleRepository, IKmHistoryRepository kmHistoryRepository,
            IKmHistoryService kmHistoryService, IMapper mapper, ICurrentUserService currentUser,
            ITransactionManager transactions)
        {
            _motorcycleRepository = motorcycleRepository;
            _kmHistoryRepository = kmHistoryRepository;
            _kmHistoryService = kmHistoryService;
            _mapper = mapper;
            _currentUser = currentUser;
            _transactions = transactions;
        }

        private void EnsureCanWrite()
        {
            if (_currentUser.IsDemo)
                throw new ForbiddenAccessException("El usuario demo solo puede visualizar información.");
        }

        public async Task<MotorcycleDTO> CreateAsync(SaveMotorcycleDTO dto)
        {
            EnsureCanWrite();
            EnsureValidImage(dto.Image);
            var entity = _mapper.Map<Motorcycle>(dto);
            entity.UserId = _currentUser.UserId;
            entity.Validate();

            // Moto + km inicial en una sola transacción: antes eran dos
            // SaveChanges separados y un fallo dejaba la moto sin historial.
            return await _transactions.ExecuteInTransactionAsync(async () =>
            {
                var created = await _motorcycleRepository.AddAsync(entity);
                var kmHistory = new MotorcycleKmHistory
                {
                    MotorcycleId = created.Id,
                    Km = dto.Km
                };
                await _kmHistoryRepository.AddAsync(kmHistory);
                await _kmHistoryRepository.SaveChangesAsync();
                var createdDto = _mapper.Map<MotorcycleDTO>(created);
                createdDto.Km = kmHistory.Km;
                return createdDto;
            });
        }

        public async Task<MotorcycleDTO?> GetByIdAsync(Guid id)
        {
            var entity = await _motorcycleRepository.GetByIdAsync(id);

            if (entity is null)
                throw new NotFoundException("Motocicleta no encontrada.");

            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes acceso a esta motocicleta.");

            var dto = _mapper.Map<MotorcycleDTO>(entity);
            dto.Km = await GetCurrentKmOrDefaultAsync(entity.Id);
            return dto;
        }

        public async Task<IList<MotorcycleDTO>> GetByCurrentUserAsync()
        {
            var motorcycles = (await _motorcycleRepository.GetByUserIdAsync(_currentUser.UserId)).ToList();
            var dtos = _mapper.Map<IList<MotorcycleDTO>>(motorcycles);

            var kmByMotorcycle = await _kmHistoryRepository.GetLatestKmByMotorcycleIdsAsync(
                motorcycles.Select(m => m.Id));

            foreach (var dto in dtos)
            {
                dto.Km = kmByMotorcycle.TryGetValue(dto.Id, out var km) ? km : 0;
            }

            return dtos;
        }

        private async Task<int> GetCurrentKmOrDefaultAsync(Guid motorcycleId)
        {
            var last = await _kmHistoryRepository.GetLastByMotorcycleIdAsync(motorcycleId);
            return last?.Km ?? 0;
        }

        public async Task<int> GetCurrentKmAsync(Guid id)
        {
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Motocicleta no encontrada.");
            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes permisos para ver esta motocicleta.");

            return await _kmHistoryService.GetCurrentKmAsync(id);
        }

        public async Task AddKmHistoryAsync(Guid id, int km)
        {
            EnsureCanWrite();
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Motocicleta no encontrada.");
            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes permisos para editar esta motocicleta.");

            await _kmHistoryService.AddKmAsync(id, km);
        }

        public async Task RollbackLastKmAsync(Guid id, int newKm)
        {
            EnsureCanWrite();
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Motocicleta no encontrada.");
            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes permisos para editar esta motocicleta.");

            await _kmHistoryService.RollbackLastKmAsync(id, newKm);
        }

        public async Task UpdateAsync(Guid id, SaveMotorcycleDTO dto)
        {
            EnsureCanWrite();
            EnsureValidImage(dto.Image);
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Motocicleta no encontrada.");

            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes permisos para editar esta motocicleta.");

            _mapper.Map(dto, entity);
            entity.Validate();
            await _motorcycleRepository.UpdateAsync(entity);
            await _motorcycleRepository.SaveChangesAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            EnsureCanWrite();
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Motocicleta no encontrada.");

            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes permisos para borrar esta motocicleta.");

            await _motorcycleRepository.SoftDeleteAsync(id);
            await _motorcycleRepository.SaveChangesAsync();
        }

        private static void EnsureValidImage(string? image)
        {
            if (string.IsNullOrWhiteSpace(image) || image == "default.png")
                return;

            const string jpegPrefix = "data:image/jpeg;base64,";
            const string pngPrefix = "data:image/png;base64,";
            const string webpPrefix = "data:image/webp;base64,";

            var prefix = image.StartsWith(jpegPrefix, StringComparison.Ordinal) ? jpegPrefix
                : image.StartsWith(pngPrefix, StringComparison.Ordinal) ? pngPrefix
                : image.StartsWith(webpPrefix, StringComparison.Ordinal) ? webpPrefix
                : null;
            if (prefix is null)
                throw new ValidationException("La imagen debe ser un data URL JPEG, PNG o WebP.");

            var payload = image[prefix.Length..];
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(payload);
            }
            catch (FormatException)
            {
                throw new ValidationException("La imagen no es válida.");
            }

            // ~1 MB decoded; the DTO length cap (1.4M chars) alone still allows
            // oversized payloads past client-side resizing.
            const int maxDecodedBytes = 1_048_576;
            if (bytes.Length == 0 || bytes.Length > maxDecodedBytes)
                throw new ValidationException("La imagen es demasiado grande.");
        }
    }
}
