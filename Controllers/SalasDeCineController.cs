using AutoMapper;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeliculasAPI.Controllers
{
    [Route("api/SalasDeCine")]
    [ApiController]
    public class SalasDeCineController: CustomBaseController
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly GeometryFactory _geometryFactory;

        public SalasDeCineController(ApplicationDbContext dbContext, IMapper mapper, GeometryFactory geometryFactory)
            :base(dbContext, mapper)
        {
            this._dbContext = dbContext;
            this._mapper = mapper;
            this._geometryFactory = geometryFactory;
        }
        [HttpGet]
        public async  Task<ActionResult<List<SalaDeCineDTO>>> Get()
        {
            return await Get<SalaDeCine, SalaDeCineDTO>();
        }

        [HttpGet("{id:int}", Name = "obtenerSalaDeCine")]
        public async Task<ActionResult<SalaDeCineDTO>> Get(int id)
        {
            return await Get<SalaDeCine, SalaDeCineDTO>(id);
        }

        [HttpGet("Cercanos")]
        public async Task<ActionResult<List<SalaDeCineCercanoDTO>>> Cercanos([FromQuery] SalaDeCineCercanoFiltroDTO salaDeCineCercanoFiltroDTO)
        {
            var ubicacionUsuario = _geometryFactory.CreatePoint(
                new Coordinate(salaDeCineCercanoFiltroDTO.Longitud, salaDeCineCercanoFiltroDTO.Latitud));

            var salasDeCine = await _dbContext.SalasDeCines.OrderBy(sc => sc.Ubicacion.Distance(ubicacionUsuario))
                .Where(u => u.Ubicacion.IsWithinDistance(ubicacionUsuario, salaDeCineCercanoFiltroDTO.DistanciaEnKms * 1000))
                .Select(x => new SalaDeCineCercanoDTO
                {
                    Id = x.Id,
                    Nombre = x.Nombre,
                    Latitud = x.Ubicacion.Y,
                    Longitud = x.Ubicacion.X,
                    DistanciaEnMetros = Math.Round(x.Ubicacion.Distance(ubicacionUsuario))
                }).ToListAsync();

            return salasDeCine;
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] SalaDeCineCreacionDTO salaDeCineCreacionDTO)
        {
            return await Post<SalaDeCineCreacionDTO, SalaDeCine, SalaDeCineDTO>(salaDeCineCreacionDTO, "obtenerSalaDeCine");
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, [FromBody] SalaDeCineCreacionDTO salaDeCineCreacionDTO)
        {
            return await Put<SalaDeCineCreacionDTO, SalaDeCine>(id, salaDeCineCreacionDTO);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            return await Delete<SalaDeCine>(id);
        }
    }
}
