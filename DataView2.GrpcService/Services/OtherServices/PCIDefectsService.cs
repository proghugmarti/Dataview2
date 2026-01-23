using DataView2.Core.Models.Other;
using DataView2.Core.Models;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models;
using DataView2.GrpcService.Data;
using System;
using DataView2.Core.Helper;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class PCIDefectsService: IPCIDefectsService
    {
        private readonly IRepository<PCIDefects> _repository;
        private readonly AppDbContextProjectData _context;
        public PCIDefectsService(IRepository<PCIDefects> repository, AppDbContextProjectData context)
        {
            _repository = repository;
            _context = context;
        }
        public async Task<IdReply> Create(PCIDefects pciDefect, CallContext context = default)
        {
            try
            {
                var entityEntry = _repository.CreateAsync(pciDefect);

                return new IdReply
                {
                    Id = entityEntry.Id,
                    Message = "New PCI Defect created."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error in Creating PCI Defect {ex.Message}"
                };
            }
        }

        public async Task<IdReply> CreateRange(List<PCIDefects> incomingDefects, CallContext context = default)
        {
            try
            {
                //Delete existing defects
                var firstDefect = incomingDefects.FirstOrDefault();
                var allDefects = await GetByPCIRatingAndSampleUnit(new PCIDefectRequest { PCIRatingId = firstDefect.PCIRatingId, SampleUnitId = firstDefect.SampleUnitId });
                var defectsToKeep = new List<PCIDefects>();
                var defectsToCreate = new List<PCIDefects>();

                foreach (var incoming in incomingDefects)
                {
                    var existingDefect = allDefects.FirstOrDefault(x => 
                                x.DefectName == incoming.DefectName &&
                                x.GeoJSON == incoming.GeoJSON && 
                                x.Qty == incoming.Qty &&
                                x.PCIRatingId == incoming.PCIRatingId && 
                                x.SampleUnitId == incoming.SampleUnitId && 
                                x.SampleUnitSetId == incoming.SampleUnitSetId);

                    if (existingDefect == null)
                    {
                        defectsToCreate.Add(incoming); //new defect
                    }
                    else
                    {
                        defectsToKeep.Add(existingDefect); //existing defect to keep
                    }
                }

                if (defectsToCreate.Count > 0)
                {
                    //Create range
                    await _repository.CreateRangeAsync(defectsToCreate);
                }

                if (allDefects.Count > 0)
                {
                    var defectsToDelete = allDefects.Where(existing => !defectsToKeep.Contains(existing)).ToList();
                    if (defectsToDelete != null && defectsToDelete.Count > 0)
                    {
                        await _repository.DeleteRangeAsync(defectsToDelete);

                        return new IdReply
                        {
                            //deleting existing graphic needs to be updated if graphics already loaded
                            Id = firstDefect.PCIRatingId,
                            Message = "PCI Defects Deleted. Graphic update needed"
                        };
                    }
                }

                return new IdReply
                {
                    Id = 0,
                    Message = "PCI Defects Created."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error in Creating Range PCI Defect {ex.Message}"
                };
            }
        }

        public async Task<IdReply> DeleteObject(PCIDefects request, CallContext context = default)
        {
            try
            {
                await _repository.DeleteAsync(request.Id);
                return new IdReply
                {
                    Id = request.Id,
                    Message = $"PCI Defect Id {request.Id} successfully edited."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error in Deleting PCI Defect {ex.Message}"
                };
            }
        }

        public async Task<IdReply> EditValue(PCIDefects request, CallContext context = default)
        {
            try
            {
                await _repository.UpdateAsync(request);
                return new IdReply
                {
                    Id = request.Id,
                    Message = $"PCI Defect {request.Id} successfully edited."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error in Editing PCI Defefct {request.Id}"
                };
            }
        }

        public async Task<List<PCIDefects>> GetAll(Empty empty, CallContext context = default)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                return entities.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<PCIDefects>();
            }
        }

        public async Task<List<PCIDefects>> GetByPCIRatingAndSampleUnit(PCIDefectRequest request, CallContext context = default)
        {
            try
            {
                var allEntities = await _repository.GetAllAsync();
                if(allEntities != null && allEntities.Count() > 0)
                {
                    var matchingEntities = allEntities.Where(x => x.PCIRatingId == request.PCIRatingId && x.SampleUnitId == request.SampleUnitId).ToList();
                    if (matchingEntities!= null && matchingEntities.Count > 0)
                    {
                        return matchingEntities;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<PCIDefects>();
        }

        public async Task<List<PCIDefects>> GetByTableName(string tableName, CallContext context = default)
        {
            var matchingDefects = new List<PCIDefects>();
            try
            {
                var allEntities = await _repository.GetAllAsync();
                if (allEntities != null)
                {
                    foreach (var entity in allEntities)
                    {
                        var name = entity.PCIRatingName + "-" + entity.DefectName;
                        if (name == tableName)
                        {
                            matchingDefects.Add(entity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return matchingDefects;
        }

        public async Task<PCIDefects> GetById(IdRequest idrequest, CallContext context = default)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(idrequest.Id);
                if(entity != null)
                {
                    return entity;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new PCIDefects();
        }
        public async Task<List<PCIDefectNameResponse>> HasData(Empty empty, CallContext context = default)
        {
            var defectNameResponse = new List<PCIDefectNameResponse>();
            try
            {
                var ratingEntities = _context.PCIRatings.ToList();
                if(ratingEntities != null && ratingEntities.Count > 0)
                {
                    foreach (var rating in ratingEntities)
                    {
                        if (rating.ProgressPercentage != 0)
                        {
                            var ratingdefectName = new PCIDefectNameResponse
                            {
                                PCIRatingName = rating.RatingName,
                                PCIDefectName = new List<string> { "Sample Unit" }
                            };
                            defectNameResponse.Add(ratingdefectName);

                            var defectNames = _context.PCIDefects.Where(x => x.PCIRatingId == rating.Id).Select(x => x.DefectName).Distinct().ToList();
                            if (defectNames != null && defectNames.Count > 0)
                            {
                                foreach (var defectName in defectNames)
                                {
                                    //Remove auto calculated defects
                                    if (!TableNameHelper.PCIAutoCalculatedDefects.Contains(defectName))
                                    {
                                        ratingdefectName.PCIDefectName.Add(defectName);
                                    }
                                }
                                defectNameResponse.Add(ratingdefectName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return defectNameResponse;
        }

        public async Task<List<PCIDefects>> GetByRatingName(string ratingName, CallContext context = default)
        {
            try
            {
                var defects = _context.PCIDefects.Where(x => x.PCIRatingName == ratingName).ToList();
                if(defects != null && defects.Count > 0)
                {
                    return defects;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return new List<PCIDefects>();
        }
    }
}
