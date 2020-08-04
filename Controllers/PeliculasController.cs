using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Servicios;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PeliculasAPI.Controllers
{
    [ApiController]
    [Route("api/peliculas")]
    public class PeliculasController : ControllerBase
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;
        private readonly IAlmacenadorArchivos _almacenadorArchivos;
        private readonly string contenedor = "peliculas";

        public PeliculasController(ApplicationDbContext applicationDbContext,
            IMapper mapper,
            IAlmacenadorArchivos almacenadorArchivos)
        {
            this._applicationDbContext = applicationDbContext;
            this._mapper = mapper;
            this._almacenadorArchivos = almacenadorArchivos;
        }

        [HttpGet]
        public async Task<ActionResult<List<PeliculaDTO>>> Get()
        {
            var peliculas = await _applicationDbContext.Peliculas.ToListAsync();
            return _mapper.Map<List<PeliculaDTO>>(peliculas);

        }

        [HttpGet("{id}", Name = "obtenerPelicula")]
        public async Task<ActionResult<PeliculaDTO>> Get(int id)
        {
            var pelicula = await _applicationDbContext.Peliculas.FirstOrDefaultAsync(p => p.Id == id);

            if (pelicula == null)
            {
                return NotFound();
            }
            return _mapper.Map<PeliculaDTO>(pelicula);
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
            if (jsonPatchDocument == null)
            {
                return BadRequest();
            }

            var entidadDB = await _applicationDbContext.Peliculas.FirstOrDefaultAsync(p => p.Id == id);
            if (entidadDB == null)
            {
                return NotFound();
            }

            var entidadDTO = _mapper.Map<PeliculaPatchDTO>(entidadDB);
            //ModelState => Microsoft.AspNetCore.Mvc.NewtonsoftJson
            jsonPatchDocument.ApplyTo(entidadDTO, ModelState);

            var esValido = TryValidateModel(entidadDTO);
            if (!esValido)
            {
                return BadRequest(ModelState);
            }

            _mapper.Map(entidadDTO, entidadDB);

            await _applicationDbContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var existe = await _applicationDbContext.Peliculas.AnyAsync(p => p.Id == id);

            if (!existe)
            {
                return NotFound();
            }

            _applicationDbContext.Remove(new Pelicula() { Id = id });
            await _applicationDbContext.SaveChangesAsync();

            return NoContent();
        }


    }
}
