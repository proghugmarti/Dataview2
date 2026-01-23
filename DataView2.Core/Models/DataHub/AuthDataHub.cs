using DataView2.Core.Models.LCMS_Data_Tables;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.DataHub
{
    [DataContract]
    public class AuthDataHub
    {
        [DataMember(Order = 1)]
        public string Username { get; set; }

        [DataMember(Order = 2)]
        public string Password { get; set; }

    }
    [DataContract]
    public class LoginResponseToken
    {
        [DataMember(Order = 1)]
        public string Token { get; set; }
    }

    [ServiceContract]
    public interface IAuthDataHubService
    {
        [OperationContract]
        Task<LoginResponseToken> AuthenticateAsync(AuthDataHub authDataRequest);
    }
}
