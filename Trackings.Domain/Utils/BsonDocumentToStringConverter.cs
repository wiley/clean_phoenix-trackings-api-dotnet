using AutoMapper;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Trackings.Domain.Utils
{
    public class BsonDocumentToStringConverter : ITypeConverter<BsonDocument, string>
    {
        public string Convert(BsonDocument source, string destination, ResolutionContext context)
        {
            if (source != null)
            {
                return source.ToJson();
            }
            return null;
        }
    }
}
