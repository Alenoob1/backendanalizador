using servicios.repository;

namespace servicios.Services
{
    public class services
    {
        public string buscardato(int id) {

            //logica de negocios
            proyectorRepository proyectorRepository = new proyectorRepository();

           var dato = proyectorRepository.BuscarDatoDB(id);

            return dato;
        }
    }
}
