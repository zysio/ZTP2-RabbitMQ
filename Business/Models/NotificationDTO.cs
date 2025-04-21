using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Models
{
    public record NotificationDTO
    (
         string Message,
         string Channel,
         string Recipient,
         string Timezone 
    );
}
