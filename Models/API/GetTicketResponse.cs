using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadRecords.Models.API
{
    public class GetTicketResponse: CommonResponse
    {
        public string? Ticket { get; set; } = null;
    }
}
