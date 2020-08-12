using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeliculasAPI.Controllers
{
    public class CustomBaseController: ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CustomBaseController(ApplicationDbContext context, IMapper mapper)
        {
            this._context = context;
            this._mapper = mapper;
        }
        protected async Task<List<TDTO>> Get<TEntidad, TDTO>() where TEntidad: class
        {
            var entidades = await _context.Set<TEntidad>().AsNoTracking().ToListAsync();
            var dtos = _mapper.Map<List<TDTO>>(entidades);
            return dtos;
        }
        protected async Task<ActionResult<TDTO>> Get<TEntidad, TDTO>(int id) where TEntidad : class, IId
        {
            var entidad = await _context.Set<TEntidad>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (entidad == null)
            {
                return NotFound();
            }
            var dto = _mapper.Map<TDTO>(entidad);
            return dto;
        }
        protected async Task<List<TDTO>> Get<TEntidad, TDTO>(PaginacionDTO paginacionDTO)
            where TEntidad: class
        {
            var queryable = _context.Set<TEntidad>().AsQueryable();
            await HttpContext.InsertarParametrosPaginacion(queryable, paginacionDTO.CantidadRegistrosPorPagina);
            var entidades = await queryable.Paginar(paginacionDTO).ToListAsync();
            return _mapper.Map<List<TDTO>>(entidades);
        }
        protected async Task<ActionResult> Post<TCreacion, TEntidad, TLectura>
            (TCreacion creacionDTO, string nombreRuta) where TEntidad: class, IId
        {
            var entidad = _mapper.Map<TEntidad>(creacionDTO);
            _context.Add(entidad);
            await _context.SaveChangesAsync();
            var  dtoLectura = _mapper.Map<TLectura>(entidad);

            return new CreatedAtRouteResult(nombreRuta, new { id = entidad.Id }, dtoLectura);
        }

        protected async Task<ActionResult> Put<TCreacion, TEntidad>
            (int id, TCreacion creacionDTO) where TEntidad : class, IId
        {
            var entidad = _mapper.Map<TEntidad>(creacionDTO);
            entidad.Id = id;
            _context.Entry(entidad).State = EntityState.Modified;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        protected async Task<ActionResult> Patch<TEntidad, TDTO>(int id, JsonPatchDocument<TDTO> patchDocument)
            where TDTO: class
            where TEntidad: class, IId
        {
            if (patchDocument == null)
            {
                return BadRequest();
            }

            var entidadDB = await _context.Set<TEntidad>().FirstOrDefaultAsync(a => a.Id == id);
            if (entidadDB == null)
            {
                return NotFound();
            }

            var entidadDTO = _mapper.Map<TDTO>(entidadDB);
            //ModelState => Microsoft.AspNetCore.Mvc.NewtonsoftJson
            patchDocument.ApplyTo(entidadDTO, ModelState);

            var esValido = TryValidateModel(entidadDTO);
            if (!esValido)
            {
                return BadRequest(ModelState);
            }

            _mapper.Map(entidadDTO, entidadDB);

            await _context.SaveChangesAsync();
            return NoContent();

        }

        protected async Task<ActionResult> Delete<TEntidad>(int id) where TEntidad: class, IId, new()
        {
            var existe = await _context.Set<TEntidad>().AnyAsync(g => g.Id == id);

            if (!existe)
            {
                return NotFound();
            }

            _context.Remove(new TEntidad() { Id = id });
            await _context.SaveChangesAsync();

            return NoContent();

        }


    }
}
