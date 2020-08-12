using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Helpers;
using PeliculasAPI.Servicios;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;
using Microsoft.Extensions.Logging;

namespace PeliculasAPI.Controllers
{
    [ApiController]
    [Route("api/peliculas")]
    public class PeliculasController : CustomBaseController
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;
        private readonly IAlmacenadorArchivos _almacenadorArchivos;
        private readonly ILogger<PeliculasController> _logger;
        private readonly string contenedor = "peliculas";

        public PeliculasController(ApplicationDbContext applicationDbContext,
            IMapper mapper,
            IAlmacenadorArchivos almacenadorArchivos,
            ILogger<PeliculasController> logger)
            :base(applicationDbContext, mapper)
        {
            this._applicationDbContext = applicationDbContext;
            this._mapper = mapper;
            this._almacenadorArchivos = almacenadorArchivos;
            this._logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PeliculasIndexDTO>> Get()
        {
            var top = 5;
            var hoy = DateTime.Today;

            var proximosEstrenos = await _applicationDbContext.Peliculas
                .Where(p => p.FechaEstreno >= hoy)
                .OrderBy(p => p.FechaEstreno)
                .Take(top)
                .ToListAsync();

            var enCines = await _applicationDbContext.Peliculas
                .Where(p => p.EnCines)
                .Take(top)
                .ToListAsync();

            var resultado = new PeliculasIndexDTO();
            resultado.FuturosEstrenos = _mapper.Map<List<PeliculaDTO>>(proximosEstrenos);
            resultado.EnCines = _mapper.Map<List<PeliculaDTO>>(enCines);

            return resultado;
        }

        [HttpGet("filtro")]
        public async Task<ActionResult<List<PeliculaDTO>>> Filtrar([FromQuery] FiltroPeliculasDTO filtroPeliculasDTO)
        {
            var peliculasQueryable = _applicationDbContext.Peliculas.AsQueryable();

            if (!string.IsNullOrEmpty(filtroPeliculasDTO.Titulo))
            {
                peliculasQueryable = peliculasQueryable.Where(p => p.Titulo.Contains(filtroPeliculasDTO.Titulo));
            }
            if (filtroPeliculasDTO.EnCines)
            {
                peliculasQueryable = peliculasQueryable.Where(p => p.EnCines);
            }
            if (filtroPeliculasDTO.ProximosEstrenos)
            {
                var hoy = DateTime.Today;
                peliculasQueryable = peliculasQueryable.Where(p => p.FechaEstreno >= hoy);
            }
            if (filtroPeliculasDTO.GeneroId != 0)
            {
                peliculasQueryable = peliculasQueryable
                    .Where(p => p.PeliculasGeneros.Select(pg => pg.GeneroId)
                    .Contains(filtroPeliculasDTO.GeneroId));
            }
            if (!string.IsNullOrEmpty(filtroPeliculasDTO.CampoOrdenar))
            {
                var tipoOrden = filtroPeliculasDTO.OrdenAscendente ? "ascending" : "descending";

                try
                {
                    peliculasQueryable = peliculasQueryable.OrderBy($"{filtroPeliculasDTO.CampoOrdenar} {tipoOrden}");
                } 
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, ex);
                }

            }
            await HttpContext.InsertarParametrosPaginacion(peliculasQueryable,
                filtroPeliculasDTO.CantidadRegistrosPorPagina);

            var peliculas = await peliculasQueryable.Paginar(filtroPeliculasDTO.Paginacion).ToListAsync();

            return _mapper.Map<List<PeliculaDTO>>(peliculas);
        }

        [HttpGet("{id}", Name = "obtenerPelicula")]
        public async Task<ActionResult<PeliculasDetalleDTO>> Get(int id)
        {
            var pelicula = await _applicationDbContext.Peliculas
                .Include(pa => pa.PeliculasActores).ThenInclude(a => a.Actor)
                .Include(pg => pg.PeliculasGeneros).ThenInclude(g => g.Genero)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pelicula == null)
            {
                return NotFound();
            }

            pelicula.PeliculasActores = pelicula.PeliculasActores.OrderBy(pa => pa.Orden).ToList();

            return _mapper.Map<PeliculasDetalleDTO>(pelicula);
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromForm] PeliculaCreacionDTO peliculaCreacionDTO)
        {
            var pelicula = _mapper.Map<Pelicula>(peliculaCreacionDTO);

            if (peliculaCreacionDTO.Poster != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await peliculaCreacionDTO.Poster.CopyToAsync(memoryStream);
                    var contenido = memoryStream.ToArray();
                    var extension = Path.GetExtension(peliculaCreacionDTO.Poster.FileName);
                    pelicula.Poster = await _almacenadorArchivos.GuardarArchivo(contenido, extension, contenedor,
                        peliculaCreacionDTO.Poster.ContentType);
                }
            }
            AsignarOrdenActores(pelicula);
            _applicationDbContext.Add(pelicula);
            await _applicationDbContext.SaveChangesAsync();
            var peliculaDto = _mapper.Map<PeliculaDTO>(pelicula);
            return new CreatedAtRouteResult("obtenerPelicula", new { id = pelicula.Id }, peliculaDto);

        }

        private void AsignarOrdenActores(Pelicula pelicula)
        {
            if (pelicula.PeliculasActores != null)
            {
                for (int i=0; i < pelicula.PeliculasActores.Count; i++)
                {
                    pelicula.PeliculasActores[i].Orden = i;
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromForm] PeliculaCreacionDTO peliculaCreacionDTO)
        {
            var peliculaDB = await _applicationDbContext.Peliculas
                .Include(pa => pa.PeliculasActores)
                .Include(pg => pg.PeliculasGeneros)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (peliculaDB == null) { return NotFound(); }

            peliculaDB = _mapper.Map(peliculaCreacionDTO, peliculaDB);

            if (peliculaCreacionDTO.Poster != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await peliculaCreacionDTO.Poster.CopyToAsync(memoryStream);
                    var contenido = memoryStream.ToArray();
                    var extension = Path.GetExtension(peliculaCreacionDTO.Poster.FileName);
                    peliculaDB.Poster = await _almacenadorArchivos.EditarArchivo(contenido, extension, contenedor,
                        peliculaDB.Poster, peliculaCreacionDTO.Poster.ContentType);
                }
            }
            AsignarOrdenActores(peliculaDB);
            await _applicationDbContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> Patch(int id, [FromBody] JsonPatchDocument<PeliculaPatchDTO> jsonPatchDocument)
        {
            return await Patch<Pelicula, PeliculaPatchDTO>(id, jsonPatchDocument);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            return await Delete<Pelicula>(id);
        }
    }
}
