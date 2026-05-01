using Microsoft.AspNetCore.Mvc;
using WarehouseIntegrationAPI.Services;

namespace WarehouseIntegrationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseController : ControllerBase
    {
        private readonly MessageProducer _messageProducer;

        public WarehouseController(MessageProducer messageProducer)
        {
            _messageProducer = messageProducer;
        }

        [HttpPost("allocate-bin")]
        public IActionResult AllocateBin([FromBody] BinAllocationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OrderId))
            {
                return BadRequest("Invalid request.");
            }

            // Publish message to queue
            _messageProducer.SendMessage("bin_allocation_queue", request);
            return Ok(new { Status = "Processing", Message = "Bin allocation initiated." });
        }

        [HttpPost("assign-order")]
        public IActionResult AssignOrder([FromBody] OrderAssignmentRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OrderId))
            {
                return BadRequest("Invalid request.");
            }

            // Publish message to queue
            _messageProducer.SendMessage("order_assignment_queue", request);
            return Ok(new { Status = "Processing", Message = "Order assignment initiated." });
        }
    }

    public class BinAllocationRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class OrderAssignmentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string DestinationHub { get; set; } = string.Empty;
    }
}
