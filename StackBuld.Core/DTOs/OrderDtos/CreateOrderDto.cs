using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackBuld.Core.DTOs.OrderDtos
{
    public class CreateOrderDto
    {
       
        public string CustomerEmail { get; set; } = string.Empty;
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
 
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateOrderItemDto
    {
    
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

}
