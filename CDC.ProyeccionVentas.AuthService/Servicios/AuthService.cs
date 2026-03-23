using CDC.ProyeccionVentas.Dominio.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.AuthService.Servicios
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public AuthService(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        // Este es el ÚNICO método de autenticación que necesitas
        public async Task<(bool Success, string Message)> ValidarCredencialesAsync(string numeroEmpleado, string passwordPlano)
        {
            try
            {
                var usuario = await _usuarioRepository.ObtenerUsuarioPorNumeroEmpleadoAsync(numeroEmpleado);

                if (usuario == null)
                {
                    System.Diagnostics.Debug.WriteLine("DEBUG: Usuario no existe.");
                    return (false, "El usuario no existe.");
                }

                if (!usuario.Activo)
                {
                    System.Diagnostics.Debug.WriteLine("DEBUG: Usuario inactivo.");
                    return (false, "El usuario está inactivo.");
                }

                var hashPassword = HashPassword(passwordPlano, usuario.Salt);

                //System.Diagnostics.Debug.WriteLine($"DEBUG: Password plano: {passwordPlano}");
                //System.Diagnostics.Debug.WriteLine($"DEBUG: Salt: {usuario.Salt}");
                //System.Diagnostics.Debug.WriteLine($"DEBUG: Hash generado: {hashPassword}");
                //System.Diagnostics.Debug.WriteLine($"DEBUG: Hash en BD: {usuario.Contrasenia}");

                if (hashPassword != usuario.Contrasenia)
                {
                    System.Diagnostics.Debug.WriteLine("DEBUG: Contraseña incorrecta.");
                    return (false, "Contraseña incorrecta.");
                }

                System.Diagnostics.Debug.WriteLine("DEBUG: Login exitoso.");
                return (true, "Login exitoso.");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: Error técnico: " + ex.Message);
                return (false, "Error técnico: " + ex.Message);
            }
        }


        // Lógica para hash, si más adelante cambian la forma de hashear, solo cambia aquí
        //private string HashPassword(string password, string salt)
        //{
        //    using (var sha1 = SHA1.Create())
        //    {
        //        byte[] bytes = Encoding.UTF8.GetBytes(password);
        //        byte[] hash = sha1.ComputeHash(bytes);
        //        return System.Convert.ToBase64String(hash);
        //    }
        //}

        private string HashPassword(string password, string salt)
        {
            // Convierte el salt desde Base64 a bytes
            byte[] saltBytes = Convert.FromBase64String(salt);

            using (var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA1))
            {
                // 20 bytes como lo define el otro sistema (usando SHA1)
                byte[] hashBytes = deriveBytes.GetBytes(20);
                return Convert.ToBase64String(hashBytes);
            }
        }

    }
}
