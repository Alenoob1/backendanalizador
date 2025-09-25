using Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace servicios.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiculoController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public VehiculoController(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // GET: api/vehiculo
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<vehiculos>>> GetAll(CancellationToken ct)
        {
            var data = await _db.Vehiculos.AsNoTracking().ToListAsync(ct);
            return Ok(data);
        }

        // GET: api/vehiculo/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<vehiculos>> GetById(int id, CancellationToken ct)
        {
            var v = await _db.Vehiculos.FindAsync(new object[] { id }, ct);
            if (v is null) return NotFound();
            return Ok(v);
        }

        // POST: api/vehiculo
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<vehiculos>> Create([FromBody] vehiculos input, CancellationToken ct)
        {
            if (input is null) return BadRequest("El vehículo no puede ser nulo.");
            _db.Vehiculos.Add(input);
            await _db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetById), new { id = input.idvehiculo }, input);
        }

        // PUT: api/vehiculo/5
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] vehiculos input, CancellationToken ct)
        {
            if (input is null) return BadRequest("El vehículo no puede ser nulo.");
            var v = await _db.Vehiculos.FindAsync(new object[] { id }, ct);
            if (v is null) return NotFound();

            v.marca = input.marca;
            v.kilometraje = input.kilometraje;
            v.precio = input.precio;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // DELETE: api/vehiculo/5
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var v = await _db.Vehiculos.FindAsync(new object[] { id }, ct);
            if (v is null) return NotFound();
            _db.Vehiculos.Remove(v);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // POST: api/vehiculo/upload-image
        [HttpPost("upload-image")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se envió ninguna imagen.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var imageBytes = ms.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config["OpenAI:ApiKey"]);

            var request = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new {
                        role = "user",
                        content = new object[]
                        {
                            new {
                                type = "text",
                                text = "Analiza la imagen de este vehículo y responde ÚNICAMENTE en formato JSON con la estructura: { \"marca\": \"string\", \"modelo\": \"string\", \"color\": \"string\", \"caracteristicas\": [\"string\"] }"
                            },
                            new {
                                type = "image_url",
                                image_url = new { url = $"data:{file.ContentType};base64,{base64Image}" }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content, ct);
            var rawResponse = await response.Content.ReadAsStringAsync(ct);

            Console.WriteLine("=== Respuesta cruda de OpenAI ===");
            Console.WriteLine(rawResponse);
            Console.WriteLine("=================================");

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new
                {
                    error = "OpenAI devolvió un error",
                    status = response.StatusCode,
                    detalle = rawResponse
                });
            }

            using var doc = JsonDocument.Parse(rawResponse);
            var contentStr = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            Console.WriteLine("=== Contenido extraído de GPT ===");
            Console.WriteLine(contentStr);
            Console.WriteLine("=================================");

            try
            {
                var vehicleData = JsonDocument.Parse(contentStr);
                return Ok(new
                {
                    resultado = vehicleData.RootElement,
                    raw = rawResponse
                });
            }
            catch (JsonException)
            {
                return Ok(new
                {
                    resultado = contentStr,
                    raw = rawResponse
                });
            }
        }
    }
}
