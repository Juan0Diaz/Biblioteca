using System;
using System.Collections.Generic;
using Npgsql;

namespace BibliotecaLibros
{
    // ═══════════════════════════════════════════
    //  MODELO: Libro
    // ═══════════════════════════════════════════
    public class Libro
    {
        // Propiedades del libro
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Autor { get; set; }
        public string Genero { get; set; }
        public int AnoPublicacion { get; set; }
        public bool Disponible { get; set; }

        public Libro() { }

        public Libro(int id, string titulo, string autor, string genero, int ano, bool disponible = true)
        {
            Id = id;
            Titulo = titulo;
            Autor = autor;
            Genero = genero;
            AnoPublicacion = ano;
            Disponible = disponible;
        }

        public override string ToString()
        {
            string estado = Disponible ? "✅ Disponible" : "❌ No disponible";
            return $"[{Id}] \"{Titulo}\" - {Autor} ({AnoPublicacion}) | Género: {Genero} | {estado}";
        }
    }

    // ═══════════════════════════════════════════
    //  SERVICIO: Biblioteca (conectada a Supabase)
    // ═══════════════════════════════════════════
    public class Biblioteca
    {
        // ⚠️ IMPORTANTE: Reemplaza estos valores con los datos de tu proyecto en Supabase.
        // Los encuentras en: Tu proyecto → "Connect" → "Session pooler"
        private readonly string _connectionString =
            "Host=db.xvhlrdgujqdcfambmojk.supabase.co;" +
            "Port=5432;" +
            "Database=postgres;" +
            "Username=postgres.xvhlrdgujqdcfambmojk;" +
            "Password=6gFolpzT9ETLOXF9;" +
            "SSL Mode=Require;";

        // ── PROBAR CONEXIÓN ───────────────────────
        public bool ProbarConexion()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ── AGREGAR ──────────────────────────────
        public Libro AgregarLibro(string titulo, string autor, string genero, int ano)
        {
            if (string.IsNullOrWhiteSpace(titulo)) throw new ArgumentException("El título no puede estar vacío.");
            if (string.IsNullOrWhiteSpace(autor)) throw new ArgumentException("El autor no puede estar vacío.");

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand(
                @"INSERT INTO libros (titulo, autor, genero, ano_publicacion, disponible)
                  VALUES (@t, @a, @g, @y, TRUE)
                  RETURNING id", conn);

            cmd.Parameters.AddWithValue("t", titulo);
            cmd.Parameters.AddWithValue("a", autor);
            cmd.Parameters.AddWithValue("g", genero ?? "Sin género");
            cmd.Parameters.AddWithValue("y", ano);

            int nuevoId = (int)cmd.ExecuteScalar();
            return new Libro(nuevoId, titulo, autor, genero ?? "Sin género", ano, true);
        }

        // ── OBTENER TODOS ─────────────────────────
        public List<Libro> ObtenerTodos()
        {
            var libros = new List<Libro>();

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand("SELECT id, titulo, autor, genero, ano_publicacion, disponible FROM libros ORDER BY id", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                libros.Add(new Libro(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetInt32(4),
                    reader.GetBoolean(5)
                ));
            }

            return libros;
        }

        // ── BUSCAR POR ID ─────────────────────────
        public Libro BuscarPorId(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand(
                "SELECT id, titulo, autor, genero, ano_publicacion, disponible FROM libros WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Libro(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetInt32(4),
                    reader.GetBoolean(5)
                );
            }

            return null;
        }

        // ── BUSCAR POR TEXTO ──────────────────────
        public List<Libro> Buscar(string termino)
        {
            var libros = new List<Libro>();
            termino = $"%{termino.ToLower()}%";

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand(
                @"SELECT id, titulo, autor, genero, ano_publicacion, disponible FROM libros
                  WHERE LOWER(titulo) LIKE @t OR LOWER(autor) LIKE @t OR LOWER(genero) LIKE @t
                  ORDER BY id", conn);
            cmd.Parameters.AddWithValue("t", termino);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                libros.Add(new Libro(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetInt32(4),
                    reader.GetBoolean(5)
                ));
            }

            return libros;
        }

        // ── ACTUALIZAR ────────────────────────────
        public bool ActualizarLibro(int id, string titulo, string autor, string genero, int ano, bool disponible)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand(
                @"UPDATE libros
                  SET titulo = @t, autor = @a, genero = @g, ano_publicacion = @y, disponible = @d
                  WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("t", titulo);
            cmd.Parameters.AddWithValue("a", autor);
            cmd.Parameters.AddWithValue("g", genero);
            cmd.Parameters.AddWithValue("y", ano);
            cmd.Parameters.AddWithValue("d", disponible);
            cmd.Parameters.AddWithValue("id", id);

            int filasAfectadas = cmd.ExecuteNonQuery();
            return filasAfectadas > 0;
        }

        // ── ELIMINAR ──────────────────────────────
        public bool EliminarLibro(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand("DELETE FROM libros WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            int filasAfectadas = cmd.ExecuteNonQuery();
            return filasAfectadas > 0;
        }
    }

    // ═══════════════════════════════════════════
    //  INTERFAZ: Menú de consola
    // ═══════════════════════════════════════════
    class Program
    {
        static Biblioteca biblioteca = new Biblioteca();

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Verificar conexión al iniciar
            Console.WriteLine("\n🔌 Conectando con Supabase...");
            if (!biblioteca.ProbarConexion())
            {
                Color(ConsoleColor.Red,
                    "❌ No se pudo conectar a la base de datos.\n" +
                    "   Verifica que los datos de conexión en _connectionString sean correctos.\n" +
                    "   (Host, Username, Password en la clase Biblioteca)\n");
                Console.WriteLine("Presiona cualquier tecla para salir...");
                Console.ReadKey();
                return;
            }

            Color(ConsoleColor.Green, "✅ Conexión exitosa con Supabase.\n");

            bool salir = false;
            while (!salir)
            {
                MostrarMenu();
                string opcion = Console.ReadLine()?.Trim();

                switch (opcion)
                {
                    case "1": ListarLibros(); break;
                    case "2": AgregarLibro(); break;
                    case "3": BuscarLibro(); break;
                    case "4": ActualizarLibro(); break;
                    case "5": EliminarLibro(); break;
                    case "6": salir = true; Despedida(); break;
                    default:
                        Color(ConsoleColor.Red, "Opción no válida. Intente de nuevo.\n");
                        break;
                }
            }
        }

        // ── MENÚ ──────────────────────────────────
        static void MostrarMenu()
        {
            Console.WriteLine();
            Color(ConsoleColor.Cyan, "╔══════════════════════════════════════╗");
            Color(ConsoleColor.Cyan, "║   📚 BIBLIOTECA DE LIBROS (Supabase)  ║");
            Color(ConsoleColor.Cyan, "╚══════════════════════════════════════╝");
            Console.WriteLine("  1. Ver todos los libros");
            Console.WriteLine("  2. Agregar libro");
            Console.WriteLine("  3. Buscar libro");
            Console.WriteLine("  4. Actualizar libro");
            Console.WriteLine("  5. Eliminar libro");
            Console.WriteLine("  6. Salir");
            Console.Write("\nElige una opción: ");
        }

        // ── LISTAR ────────────────────────────────
        static void ListarLibros()
        {
            try
            {
                var libros = biblioteca.ObtenerTodos();
                Console.WriteLine();

                if (libros.Count == 0)
                {
                    Color(ConsoleColor.Yellow, "La biblioteca está vacía. ¡Agrega tu primer libro!");
                    return;
                }

                Color(ConsoleColor.Green, $"═══ {libros.Count} libro(s) en la biblioteca ═══");
                foreach (var libro in libros)
                    Console.WriteLine("  " + libro);
            }
            catch (Exception ex)
            {
                Color(ConsoleColor.Red, $"❌ Error al obtener libros: {ex.Message}");
            }
        }

        // ── AGREGAR ──────────────────────────────
        static void AgregarLibro()
        {
            Console.WriteLine("\n─── Agregar nuevo libro ───");
            string titulo = Leer("Título");
            string autor = Leer("Autor");
            string genero = LeerOpcional("Género (opcional)");
            int ano = LeerEntero("Año de publicación", 1000, DateTime.Now.Year);

            try
            {
                var libro = biblioteca.AgregarLibro(titulo, autor, genero, ano);
                Color(ConsoleColor.Green, $"\n✅ Libro guardado en Supabase con ID {libro.Id}:");
                Console.WriteLine("   " + libro);
            }
            catch (Exception ex)
            {
                Color(ConsoleColor.Red, $"\n❌ Error al agregar: {ex.Message}");
            }
        }

        // ── BUSCAR ────────────────────────────────
        static void BuscarLibro()
        {
            Console.Write("\nIngresa término de búsqueda (título, autor o género): ");
            string termino = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(termino))
            {
                Color(ConsoleColor.Yellow, "Debes ingresar un término de búsqueda.");
                return;
            }

            try
            {
                var resultados = biblioteca.Buscar(termino);

                if (resultados.Count == 0)
                    Color(ConsoleColor.Yellow, "No se encontraron libros con ese término.");
                else
                {
                    Color(ConsoleColor.Green, $"\n{resultados.Count} resultado(s) encontrado(s):");
                    foreach (var l in resultados) Console.WriteLine("  " + l);
                }
            }
            catch (Exception ex)
            {
                Color(ConsoleColor.Red, $"❌ Error al buscar: {ex.Message}");
            }
        }

        // ── ACTUALIZAR ────────────────────────────
        static void ActualizarLibro()
        {
            Console.WriteLine("\n─── Actualizar libro ───");
            int id = LeerEntero("ID del libro a actualizar", 1, int.MaxValue);

            Libro libro;
            try { libro = biblioteca.BuscarPorId(id); }
            catch (Exception ex) { Color(ConsoleColor.Red, $"❌ Error: {ex.Message}"); return; }

            if (libro == null) { Color(ConsoleColor.Red, "❌ No se encontró ningún libro con ese ID."); return; }

            Console.WriteLine($"\nLibro actual: {libro}");
            Console.WriteLine("(Deja en blanco para conservar el valor actual)\n");

            string titulo = LeerOpcional($"Nuevo título [{libro.Titulo}]");
            string autor = LeerOpcional($"Nuevo autor  [{libro.Autor}]");
            string genero = LeerOpcional($"Nuevo género [{libro.Genero}]");
            string anoStr = LeerOpcional($"Nuevo año    [{libro.AnoPublicacion}]");

            int ano = int.TryParse(anoStr, out int a) ? a : libro.AnoPublicacion;

            Console.Write($"¿Disponible? (s/n) [{(libro.Disponible ? "s" : "n")}]: ");
            string dispStr = Console.ReadLine()?.Trim().ToLower();
            bool disponible = dispStr == "s" ? true : dispStr == "n" ? false : libro.Disponible;

            // Usar valores actuales si se dejó en blanco
            if (string.IsNullOrWhiteSpace(titulo)) titulo = libro.Titulo;
            if (string.IsNullOrWhiteSpace(autor)) autor = libro.Autor;
            if (string.IsNullOrWhiteSpace(genero)) genero = libro.Genero;

            try
            {
                bool ok = biblioteca.ActualizarLibro(id, titulo, autor, genero, ano, disponible);
                if (ok)
                {
                    var actualizado = biblioteca.BuscarPorId(id);
                    Color(ConsoleColor.Green, $"\n✅ Libro actualizado en Supabase:");
                    Console.WriteLine("   " + actualizado);
                }
                else
                    Color(ConsoleColor.Yellow, "No se realizaron cambios.");
            }
            catch (Exception ex)
            {
                Color(ConsoleColor.Red, $"❌ Error al actualizar: {ex.Message}");
            }
        }

        // ── ELIMINAR ──────────────────────────────
        static void EliminarLibro()
        {
            Console.WriteLine("\n─── Eliminar libro ───");
            int id = LeerEntero("ID del libro a eliminar", 1, int.MaxValue);

            Libro libro;
            try { libro = biblioteca.BuscarPorId(id); }
            catch (Exception ex) { Color(ConsoleColor.Red, $"❌ Error: {ex.Message}"); return; }

            if (libro == null) { Color(ConsoleColor.Red, "❌ No se encontró ningún libro con ese ID."); return; }

            Console.WriteLine($"\nLibro a eliminar: {libro}");
            Console.Write("¿Estás seguro? Esta acción no se puede deshacer. (s/n): ");

            if (Console.ReadLine()?.Trim().ToLower() == "s")
            {
                try
                {
                    biblioteca.EliminarLibro(id);
                    Color(ConsoleColor.Green, "✅ Libro eliminado de Supabase correctamente.");
                }
                catch (Exception ex)
                {
                    Color(ConsoleColor.Red, $"❌ Error al eliminar: {ex.Message}");
                }
            }
            else
            {
                Color(ConsoleColor.Yellow, "Operación cancelada.");
            }
        }

        // ── HELPERS ───────────────────────────────
        static string Leer(string campo)
        {
            string valor;
            do
            {
                Console.Write($"{campo}: ");
                valor = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(valor))
                    Color(ConsoleColor.Red, $"  El campo '{campo}' es obligatorio.");
            } while (string.IsNullOrEmpty(valor));
            return valor;
        }

        static string LeerOpcional(string prompt)
        {
            Console.Write($"{prompt}: ");
            return Console.ReadLine()?.Trim();
        }

        static int LeerEntero(string campo, int min, int max)
        {
            while (true)
            {
                Console.Write($"{campo}: ");
                if (int.TryParse(Console.ReadLine(), out int valor) && valor >= min && valor <= max)
                    return valor;
                Color(ConsoleColor.Red, $"  Por favor ingresa un número válido entre {min} y {max}.");
            }
        }

        static void Color(ConsoleColor color, string texto)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(texto);
            Console.ResetColor();
        }

        static void Despedida()
        {
            Color(ConsoleColor.Cyan, "\n¡Hasta luego! 📚\n");
        }
    }
}