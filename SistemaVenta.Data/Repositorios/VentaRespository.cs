using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Abstractions;
using SistemaVenta.Data.DBcontext;
using SistemaVenta.Data.Repositorios.Contrato;
using SistemaVenta.Model;
namespace SistemaVenta.Data.Repositorios
{
    public class VentaRespository : GenericRepository<Venta>, IVentaRepository
    {
        private readonly DbventaContext _dbContext;
        public VentaRespository( DbventaContext dbContext ): base(dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<Venta> Registrar(Venta model)
        {
            Venta ventaGenerada = new Venta();
            using (var transaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    foreach (DetalleVenta dv in model.DetalleVenta)
                    {
                        Producto p_encontrado = _dbContext.Productos.Where(p => p.IdProducto == dv.IdProducto).First();
                        p_encontrado.Stock = p_encontrado.Stock - dv.Cantidad;
                        _dbContext.Productos.Update(p_encontrado);
                    }
                    await _dbContext.SaveChangesAsync();

                    NumeroDocumento correlativo = _dbContext.NumeroDocumentos.First();

                    correlativo.UltimoNumero = correlativo.UltimoNumero + 1;
                    correlativo.FechaRegistro = DateTime.Now;

                    _dbContext.NumeroDocumentos.Update(correlativo);
                    await _dbContext.SaveChangesAsync();

                    int cantidadDigitos = 4;
                    string ceros = string.Concat(Enumerable.Repeat("0", cantidadDigitos));
                    string numeroVenta = ceros + correlativo.UltimoNumero.ToString();
                    numeroVenta = numeroVenta.Substring(numeroVenta.Length - cantidadDigitos, cantidadDigitos);

                    model.NumeroDocumento = numeroVenta;

                    await _dbContext.Venta.AddAsync(model);
                    await _dbContext.SaveChangesAsync();

                    ventaGenerada = model;
                    transaction.Commit();
                }catch  {
                    transaction.Rollback();
                    throw;
                }
                return ventaGenerada;
            }
        }

    }
}
