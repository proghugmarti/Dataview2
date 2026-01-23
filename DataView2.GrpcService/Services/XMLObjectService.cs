using DataView2.Core;
using DataView2.Core.Models;
using DataView2.Core.Models.DTS;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;

namespace DataView2.GrpcService.Services
{
    public class XMLObjectService : BaseService<XMLObject, IRepository<XMLObject>>, IXMLObjectService
    {
        private readonly AppDbContextProjectData _context;
        public XMLObjectService(IRepository<XMLObject> repository, AppDbContextProjectData context) : base(repository)
        {
            _context = context;
        }

        public async Task<IdReply> ProcessXMLFile(string xmlContent, CallContext context = default)
        {
            IdReply reply = new IdReply();

            List<XMLObject> xmlObjects = new List<XMLObject>();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);

            await ParseXmlNodeAsync(xmlDoc.DocumentElement, "", xmlObjects, 0);

            //return xmlObjects;

            return reply;
        }

        private async Task ParseXmlNodeAsync(XmlNode node, string parentName, List<XMLObject> xmlObjects, int level)
        {
            if (node == null)
                return;

            XMLObject xmlObject = new XMLObject
            {
                Name = node.Name,
                Parent = parentName,
                Type = node.NodeType.ToString(),
                Level = level
            };

            xmlObjects.Add(xmlObject);
            
            IdReply reply = await Create(xmlObject);

            foreach (XmlNode childNode in node.ChildNodes)
            {
                _ = ParseXmlNodeAsync(childNode, node.Name, xmlObjects, level + 1);
            }
        }
    }
}
