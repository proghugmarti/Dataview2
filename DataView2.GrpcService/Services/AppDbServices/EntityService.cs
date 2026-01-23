using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static DataView2.Core.Helper.TableViewHelper;

namespace DataView2.GrpcService.Services.AppDbServices
{
    public class EntityService : IEntityService
    {
        private readonly AppDbContextProjectData _context;
   

        public EntityService(AppDbContextProjectData context)
        {
            _context = context;
        }

        //public async Task<EntityResponse> GetPagedData(EntityRequest request)
        //{
        //    //Type entityType = Type.GetType(request.EntityType);
        //    //if (entityType == null) return new EntityResponse();


        //    //var dbSet = _context.Set(entityType);
        //    //int totalCount = await dbSet.CountAsync();
        //    //var data = await dbSet.Skip(request.Page * request.PageSize)
        //    //                      .Take(request.PageSize)
        //    //                      .ToListAsync();

        //    //var response = new EntityResponse
        //    //{
        //    //    TotalCount = totalCount,
        //    //    Data = data.Select(item =>
        //    //        entityType.GetProperties()
        //    //                  .ToDictionary(p => p.Name, p => p.GetValue(item)))
        //    //                  .ToList()
        //    //};

        //    //return response;
        //    return null;
        //}


        //public async Task UpdateEntity(Dictionary<string, object> entityData, string entityType)
        //{
        //    Type type = Type.GetType(entityType);
        //    var entity = Activator.CreateInstance(type);

        //    foreach (var kvp in entityData)
        //    {
        //        var prop = type.GetProperty(kvp.Key);
        //        if (prop != null)
        //            prop.SetValue(entity, Convert.ChangeType(kvp.Value, prop.PropertyType));
        //    }

        //    _context.Update(entity);
        //    await _context.SaveChangesAsync();
        //}
    }

}
