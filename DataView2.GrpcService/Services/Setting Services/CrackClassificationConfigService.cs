using DataView2.Core.Models.Setting;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;

namespace DataView2.GrpcService.Services.Setting_Services
{
    public class CrackClassificationConfigService : ICrackClassificationConfiguration
    {
        private readonly IRepository<CrackClassificationConfiguration> _repository;

        public CrackClassificationConfigService(IRepository<CrackClassificationConfiguration> repository)
        {
            _repository = repository;
        }
        public async Task<CrackClassificationConfiguration> EditClassification(CrackClassificationConfiguration request, CallContext context = default)
        {
            var existingClassification = await _repository.GetByIdAsync(1);

            if (existingClassification != null)
            {
                // Update the existing classification with the values from the request
                existingClassification.MinSizeToStraight = request.MinSizeToStraight;
                existingClassification.MinSizeToAvoidMerge = request.MinSizeToAvoidMerge;
                existingClassification.Straightness = request.Straightness;
                existingClassification.MinimumDeep = request.MinimumDeep;
                existingClassification.LowThreshold = request.LowThreshold;
                existingClassification.LowMediumThreshold = request.LowMediumThreshold;
                existingClassification.MediumHighThreshold = request.MediumHighThreshold;
                existingClassification.HighThreshold = request.HighThreshold;
                existingClassification.IgnoreOutLanes = request.IgnoreOutLanes;
                existingClassification.ConfigFilePath = request.ConfigFilePath;

                var updatedClassification = await _repository.UpdateAsync(existingClassification);

                return updatedClassification;
            }
            else
            {
                var newClassification = new CrackClassificationConfiguration
                {
                    Id = 1,
                    MinSizeToStraight = request.MinSizeToStraight,
                    MinSizeToAvoidMerge = request.MinSizeToAvoidMerge,
                    Straightness = request.Straightness,
                    MinimumDeep = request.MinimumDeep,
                    LowThreshold = request.LowThreshold,
                    LowMediumThreshold = request.LowMediumThreshold,
                    MediumHighThreshold = request.MediumHighThreshold,
                    HighThreshold = request.HighThreshold,
                    IgnoreOutLanes = request.IgnoreOutLanes
                };

                await _repository.CreateAsync(newClassification);

                return newClassification;
            }
        }

        public async Task<CrackClassificationConfiguration> GetClassification(Empty empty, CallContext context = default)
        {
            var entity = await _repository.GetByIdAsync(1);

            // If entity doesn't exist, create new configuration
            if (entity == null)
            {
                var newClassification = new CrackClassificationConfiguration
                {
                    Id = 1,
                    MinSizeToStraight = 4,
                    MinSizeToAvoidMerge = 6,
                    Straightness = 0.7,
                    MinimumDeep = 0,
                    LowThreshold = 0,
                    LowMediumThreshold = 0,
                    MediumHighThreshold = 0,
                    HighThreshold = 0,
                    IgnoreOutLanes = true
                };
                await _repository.CreateAsync(newClassification);
                return newClassification;
            }

            return entity;
        }
    }
}
