using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class PCIRatingService : IPCIRatingService
    {
        private readonly IRepository<PCIRatings> _repository;
        private readonly AppDbContextProjectData _context;
        public PCIRatingService(IRepository<PCIRatings> repository, AppDbContextProjectData context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<IdReply> Create(PCIRatings pciRating, CallContext context = default)
        {
            try
            {
                var entityEntry = await _repository.CreateAsync(pciRating);

                //Save RatingStatus at the same time
                var sampleUnitSet = _context.SampleUnit_Set.FirstOrDefault(x => x.Id == pciRating.SampleUnitSetId);
                if (sampleUnitSet != null)
                {
                    await _context.Entry(sampleUnitSet).Collection(s => s.SampleUnits).LoadAsync();

                    if (sampleUnitSet.SampleUnits != null)
                    {
                        foreach (var sampleUnit in sampleUnitSet.SampleUnits)
                        {
                            //create a rating status
                            var ratingStatus = new PCIRatingStatus
                            {
                                PCIRatingId = entityEntry.Id,
                                SampleUnitId = sampleUnit.Id,
                                Status = false,
                            };

                            _context.PCIRatingStatus.Add(ratingStatus);
                        }
                        _context.SaveChanges();
                    }
                }

                return new IdReply
                {
                    Id = entityEntry.Id,
                    Message = "New PCI Rating created."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error in Creating PCI Rating {ex.Message}"
                };
            }
        }

        public async Task<IdReply> DeleteObject(PCIRatings request, CallContext context = default)
        {
            try
            {
                await _repository.DeleteAsync(request.Id);
                return new IdReply
                {
                    Id = request.Id,
                    Message = $"PCI Rating Id {request.Id} successfully edited."
                };
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error in Deleting PCI Rating {ex.Message}"
                };
            }
        }

        public async Task<IdReply> EditValue(PCIRatings request, CallContext context = default)
        {
            try
            {
                await _repository.UpdateAsync(request);
                return new IdReply
                {
                    Id = request.Id,
                    Message = $"PCI Rating {request.Id} successfully edited."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error in Editing PCI Rating{request.Id}"
                };
            }
        }

        public async Task<List<PCIRatings>> GetAll(Empty empty, CallContext context = default)
        {
            try
            {
                var pciRatings = _context.PCIRatings.ToList(); // Fetch PCIRating list

                foreach (var rating in pciRatings)
                {
                    // Explicitly load related SampleUnitSet and PCIDefects for each PCIRating
                    await _context.Entry(rating).Reference(p => p.SampleUnitSet).LoadAsync();
                    await _context.Entry(rating).Collection(p => p.PCIDefects).LoadAsync();
                }
                return pciRatings;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<PCIRatings>();
            }
        }

        public async Task<PCIRatings> GetByName(string ratingName, CallContext context = default)
        {
            try
            {
                var pciRating = await _repository.FirstOrDefaultAsync(x => x.RatingName == ratingName);
                if(pciRating != null)
                {
                    await _context.Entry(pciRating).Collection(p => p.PCIDefects).LoadAsync();

                    return pciRating;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return new PCIRatings();
        }

        public async Task<PCIRatings> GetById(IdRequest idRequest, CallContext context = default)
        {
            try
            {
                var pciRating = await _repository.GetByIdAsync(idRequest.Id);
                if (pciRating != null)
                {
                    await _context.Entry(pciRating).Collection(p => p.PCIDefects).LoadAsync();

                    return pciRating;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return new PCIRatings();
        }

        public async Task UpdateRatingStatus(PCIRatingStatus ratingStatus, CallContext context = default)
        {
            try
            {
                //Retrieve all relevant entities
                var entities = _context.PCIRatingStatus.Where(x => x.PCIRatingId == ratingStatus.PCIRatingId).ToList();

                // If no entities exist, create rating status
                if (!entities.Any())
                {
                    _context.PCIRatingStatus.Add(ratingStatus);
                    entities.Add(ratingStatus);
                }
                else
                {
                    // Update RatingStatus for the specific SampleUnitId
                    var entity = entities.FirstOrDefault(x => x.SampleUnitId == ratingStatus.SampleUnitId);
                    if (entity != null)
                    {
                        entity.Status = ratingStatus.Status;
                    }
                    else
                    {
                        _context.PCIRatingStatus.Add(ratingStatus);
                        entities.Add(ratingStatus);
                    }
                }

                // Recalculate status percentage
                int statusTrueCount = entities.Count(x => x.Status == true);
                double statusPercentage = (double)statusTrueCount / entities.Count * 100;

                // Update ProgressPercentage in PCIRating
                var ratingEntity = await _repository.GetByIdAsync(ratingStatus.PCIRatingId);
                if (ratingEntity != null)
                {
                    ratingEntity.ProgressPercentage = statusPercentage;
                    _context.Entry(ratingEntity).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }

                // Save changes
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating rating status: {ex.Message}");
            }
        }

        public async Task<List<PCIRatingStatus>> GetRatingStatus(IdRequest pciRatingId, CallContext context = default)
        {
            try
            {
                var status = _context.PCIRatingStatus.Where(x => x.PCIRatingId == pciRatingId.Id).ToList();
                if (status != null)
                {
                    return status;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return new List<PCIRatingStatus>();
        }
    }
}
