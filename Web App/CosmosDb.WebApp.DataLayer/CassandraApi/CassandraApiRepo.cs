using Cassandra;
using Cassandra.Mapping;
using CosmosDb.WebApp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace CosmosDb.WebApp.DataLayer.CassandraApi
{
	public static class CassandraApiRepo
    {
        public static string CreateMoviesTable(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            using (var session = CreateCassandraSession(config))
            {
                // Create keyspace (database)
                session.Execute("DROP KEYSPACE IF EXISTS movies");
                session.Execute("CREATE KEYSPACE movies WITH REPLICATION = { 'class' : 'NetworkTopologyStrategy', 'datacenter1' : 1 };");
                session.ChangeKeyspace("movies");

                // Create table
                session.Execute(@"
                    CREATE TABLE movie (
                        genre text,
                        title text PRIMARY KEY,
                        year int,
                        length text,
                        description text
                    )
                ");

                // Load two movies
                var mapper = new Mapper(session);

                var movie = new Movie("sci-fi", "Star Wars IV - A New Hope")
                {
                    Year = 1977,
                    Length = "2hr, 1min",
                    Description = "Luke Skywalker joins forces with a Jedi Knight, a cocky pilot, a Wookiee and two droids to save the galaxy from the Empire's world-destroying battle-station while also attempting to rescue Princess Leia from the evil Darth Vader."
                };
                mapper.Insert(movie);

                movie = new Movie("sci-fi", "Star Wars V - The Empire Strikes Back")
                {
                    Year = 1980,
                    Length = "2hr, 4min",
                    Description = "After the rebels are overpowered by the Empire on the ice planet Hoth, Luke Skywalker begins Jedi training with Yoda. His friends accept shelter from a questionable ally as Darth Vader hunts them in a plan to capture Luke."
                };
                mapper.Insert(movie);

                movie = new Movie("drama", "The Godfather")
                {
                    Year = 1972,
                    Length = "2hr, 55min",
                    Description = "Widely regarded as one of the greatest films of all time, this mob drama, based on Mario Puzo's novel of the same name, focuses on the powerful Italian-American crime family of Don Vito Corleone (Marlon Brando). When the don's youngest son, Michael (Al Pacino), reluctantly joins the Mafia, he becomes involved in the inevitable cycle of violence and betrayal. Although Michael tries to maintain a normal relationship with his wife, Kay (Diane Keaton), he is drawn deeper into the family business."
                };
                mapper.Insert(movie);

                return "Created Movies table with 3 movie rows";
            }
        }

        public static IEnumerable<Movie> QueryAllMovies(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            using var session = CreateCassandraSession(config);
            session.ChangeKeyspace("movies");
            var mapper = new Mapper(session);
            var movies = mapper.Fetch<Movie>("SELECT * FROM movie");

            return movies.ToList();
        }

        public static IEnumerable<Movie> QuerySciFiMovies(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			using var session = CreateCassandraSession(config);
			session.ChangeKeyspace("movies");
			var mapper = new Mapper(session);
			var movies = mapper.Fetch<Movie>("SELECT * FROM movie WHERE genre = ? ALLOW FILTERING", "sci-fi");

			return movies.ToList();
		}

        public static IEnumerable<Movie> QueryStarWarsVI(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			using var session = CreateCassandraSession(config);
			session.ChangeKeyspace("movies");
			var mapper = new Mapper(session);
			var movies = mapper.Fetch<Movie>("SELECT * FROM movie WHERE title = ? ALLOW FILTERING", "Star Wars VI - Return of the Jedi");

			return movies.ToList();
		}

        public static string CreateMovieRow(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			using var session = CreateCassandraSession(config);
			session.ChangeKeyspace("movies");

			var mapper = new Mapper(session);
			var movie = new Movie("sci-fi", "Star Wars VI - Return of the Jedi")
			{
				Year = 1983,
				Length = "2hr, 11min",
				Description = "After a daring mission to rescue Han Solo from Jabba the Hutt, the rebels dispatch to Endor to destroy a more powerful Death Star. Meanwhile, Luke struggles to help Vader back from the dark side without falling into the Emperor's trap."
			};
			mapper.Insert(movie);

			return "Created new movie row";
		}

        public static string DeleteMovieRow(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            using (var session = CreateCassandraSession(config))
            {
                session.ChangeKeyspace("movies");
                session.Execute("DELETE FROM movie WHERE title = 'Star Wars VI - Return of the Jedi'");
            }

            return "Deleted new movie row";
        }

        public static string DeleteMoviesTable(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            using (var session = CreateCassandraSession(config))
            {
                session.Execute("DROP TABLE movies.movie");
                session.Execute("DROP KEYSPACE movies");
            }

            return "Deleted Movies table";
        }

        private static ISession CreateCassandraSession(AppConfig config)
        {
            var options = new SSLOptions(SslProtocols.Tls12, true, ValidateServerCertificate);
            options.SetHostNameResolver((ipAddress) => config.CassandraApiContactPoint);
            var cluster = Cluster.Builder()
                .WithCredentials(config.CassandraApiUsername, config.CassandraApiPassword)
                .WithPort(config.CassandraApiPortNumber)
                .AddContactPoint(config.CassandraApiContactPoint)
                .WithSSL(options)
                .Build();

            var session = cluster.Connect();

            return session;
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            // Do not allow this client to communicate with unauthenticated servers.
            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            return false;
        }

    }

}
