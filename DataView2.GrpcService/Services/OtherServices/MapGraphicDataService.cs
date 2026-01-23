using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class MapGraphicDataService: IMapGraphicDataService
    {
        private readonly IRepository<MapGraphicData> _repository;
        private readonly AppDbContextMetadata _context;
        public MapGraphicDataService(IRepository<MapGraphicData> repository, AppDbContextMetadata context) 
        {
            _repository = repository;
            _context = context;
        }

        public async Task<IdReply> Create(MapGraphicData request, CallContext context = default)
        {
            var entityEntry = await _repository.CreateAsync(request);

            return new IdReply
            {
                Id = entityEntry.Id,  
                Message = "New Graphic Data Created."
            };
        }

        public async Task<MapGraphicData> Edit(MapGraphicData request, CallContext callContext = default)
        {
            try
            {
                var entity =  await _repository.UpdateAsync(request);
                return entity;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new MapGraphicData();
        }

        public async Task<List<MapGraphicData>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return entities.ToList();
        }

        public async Task<MapGraphicData> GetByName(NameRequest nameRequest, CallContext context = default)
        {
            var name = nameRequest.Name;
            var entities = await _repository.GetAllAsync();
            if (entities.Any())
            {
                // Use LINQ to filter entities that start with the given name
                // Optionally use Regex if needed for more complex patterns
                var matchingEntities = entities.Where(entity => entity.Name.StartsWith(name)).ToList();
                // Return the first matching entity if any exist
                if (matchingEntities.Any())
                {
                    return matchingEntities.First();
                }
            }
            return new MapGraphicData();
        }
    }
}
