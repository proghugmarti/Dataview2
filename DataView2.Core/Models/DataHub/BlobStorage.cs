using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.DataHub
{
    internal class BlobStorage
    {
    }

    public interface IBlobStorageService
    {
        Task<string> UploadImageAsync(Stream imageStream, string fileName);
    }
}
