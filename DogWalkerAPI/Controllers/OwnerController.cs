﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DogWalkerAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DogWalkerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OwnerController : ControllerBase
    {
        private readonly IConfiguration _config;

        public OwnerController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }


        // Get all departments from the database
        [HttpGet]
        public async Task<IActionResult> GET(
            [FromQuery] string name,
            [FromQuery] string address,
            [FromQuery] string phone
            )
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT o.Id, o.Name, o.Address, o.NeighborhoodId, o.Phone, n.Name AS NeighborhoodName
                        FROM Owner o
                        LEFT JOIN Neighborhood n
                        ON n.Id = o.NeighborhoodId
                        WHERE 1 = 1
                        ";

                    if (name != null)
                    {
                        cmd.CommandText += " AND o.Name LIKE @name";
                        cmd.Parameters.Add(new SqlParameter("@name", "%" + name + "%"));
                    }

                    if (address != null)
                    {
                        cmd.CommandText += " AND o.Address LIKE @address";
                        cmd.Parameters.Add(new SqlParameter("@address", "%" + address + "%"));
                    }

                    if (phone != null)
                    {
                        cmd.CommandText += " AND o.Phone LIKE @phone";
                        cmd.Parameters.Add(new SqlParameter("@phone", "%" + phone + "%"));
                    }

                    SqlDataReader reader = cmd.ExecuteReader();

                    var owner = new List<Owner>();

                    while (reader.Read())
                    {
                        if (owner == null)
                        {

                        }

                        var newOwner = new Owner
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Address = reader.GetString(reader.GetOrdinal("Address")),
                            NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                            Phone = reader.GetString(reader.GetOrdinal("Phone")),
                            Neighborhood = new Neighborhood()
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("NeighborhoodName"))
                            }
                        };

                        owner.Add(newOwner);
                    }
                    reader.Close();

                    return Ok(owner);
                }
            }
        }


        // Get a single department by Id from database
        [HttpGet("{id}", Name = "GetOwner")]
        public async Task<IActionResult> GET([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT o.Id, o.Name, o.Address, o.NeighborhoodId, o.Phone, d.Id AS DogId, d.Name AS DogName, d.Breed, d.OwnerId
                        FROM Owner o
                        LEFT JOIN Dog d
                        ON o.Id = d.OwnerId
                        WHERE o.Id = @id";

                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();

                    Owner owner = null;

                    while (reader.Read())
                    {
                        if (owner == null)
                        {
                            owner = new Owner
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Address = reader.GetString(reader.GetOrdinal("Address")),
                                NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                                Phone = reader.GetString(reader.GetOrdinal("Phone")),
                                Dogs = new List<Dog>()
                            };
                        }

                        owner.Dogs.Add(new Dog()
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("DogId")),
                            Name = reader.GetString(reader.GetOrdinal("DogName")),
                            Breed = reader.GetString(reader.GetOrdinal("Breed")),
                            OwnerId = reader.GetInt32(reader.GetOrdinal("OwnerId"))
                        });

                    }
                    reader.Close();

                    return Ok(owner);
                }
            }
        }


        // Create department and add it to database
        [HttpPost]
        public async Task<IActionResult> POST([FromBody] Owner owner)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Owner (Name, Address, NeighborhoodId, Phone)
                                        OUTPUT INSERTED.Id
                                        VALUES (@name, @address, @neighborhoodId, @phone)";
                    cmd.Parameters.Add(new SqlParameter("@name", owner.Name));
                    cmd.Parameters.Add(new SqlParameter("@address", owner.Address));
                    cmd.Parameters.Add(new SqlParameter("@neighborhoodId", owner.NeighborhoodId));
                    cmd.Parameters.Add(new SqlParameter("@phone", owner.Phone));

                    int newId = (int)cmd.ExecuteScalar();
                    owner.Id = newId;
                    return CreatedAtRoute("GetOwner", new { id = newId }, owner);
                }
            }
        }

        //Update single department by id in database
        [HttpPut("{id}")]
        public async Task<IActionResult> PUT([FromRoute] int id, [FromBody] Owner owner)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Owner
                                            SET 
                                            Name = @name,
                                            Address = @address,
                                            NeighborhoodId = @neighborhoodId,
                                            Phone = @phone
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@name", owner.Name));
                        cmd.Parameters.Add(new SqlParameter("@address", owner.Address));
                        cmd.Parameters.Add(new SqlParameter("@neighborhoodId", owner.NeighborhoodId));
                        cmd.Parameters.Add(new SqlParameter("@phone", owner.Phone));
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!DepartmentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // Delete single department by id from database
        [HttpDelete("{id}")]
        public async Task<IActionResult> DELETE([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM Owner WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!DepartmentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }


        // Check to see if department exist by id in database
        private bool DepartmentExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, Name, Address, NeighborhoodId, Phone
                        FROM Owner
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
    }
}