﻿namespace AddressProviderFramework.Implementations.Ptt
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using AddressProviderFramework.Models;
    using ExcelDataReader;

    public class PttAddressProviderRepository : IAddressProviderRepository
    {
        private Country DefaultCountry { get; set; }
        private List<Country> Countries { get; set; } = new List<Country>();

        /// <summary>
        /// Initializes repository with an excel file. Should be added to services layer as a singleton service.
        /// </summary>
        /// <param name="filePath"></param>
        public void Initialize(string filePath)
        {
            this.DefaultCountry = new Country
            {
                Name = "TÜRKİYE"
            };

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            Countries.Add(this.DefaultCountry);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // Skip header row
                    reader.Read();

                    do
                    {
                        while (reader.Read())
                        {
                            var state = reader.GetString(0).Trim();
                            var city = reader.GetString(1).Trim();
                            var district = reader.GetString(2).Trim();
                            var neighborhood = reader.GetString(3).Trim();
                            var postalCode = reader.GetString(4).Trim();

                            this.HandleRow(state, city, district, neighborhood, postalCode);
                        }
                    } while (reader.NextResult());
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<Country> GetCountries()
        {
            return this.Countries;
        }

        /// <inheritdoc/>
        public IEnumerable<State> GetStates(string country)
        {
            return this.DefaultCountry.GetStates();
        }

        /// <inheritdoc/>
        public IEnumerable<City> GetCities(string country, string state)
        {
            return this.DefaultCountry.GetState(state)?.GetCities() ?? new List<City>();
        }

        /// <inheritdoc/>
        public IEnumerable<District> GetDistricts(string country, string state, string city = null)
        {
            var stateObj = this.DefaultCountry.GetState(state);

            if(stateObj == null)
            {
                return new List<District>();
            }

            if (string.IsNullOrEmpty(city))
            {
                return stateObj.GetCities().SelectMany(c => c.GetDistricts());
            }
            else
            {
                return stateObj.GetCity(city)?.GetDistricts() ?? new List<District>();
            }
        }

        public IEnumerable<Neighborhood> GetNeighborhoods(string country, string state, string city, string district = null)
        {
            var c = this.DefaultCountry.GetState(state)?.GetCity(city);

            if(c == null)
            {
                return new List<Neighborhood>();
            }

            if(!string.IsNullOrEmpty(district))
            {
                return c.GetDistrict(district)?.GetNeighborhoods() ?? new List<Neighborhood>();
            }
            else
            {
                return c.GetDistricts().SelectMany(d => d.GetNeighborhoods());
            }
        }

        private void HandleRow(string stateStr, string cityStr, string districtStr, string neighborhoodStr, string postalCodeStr)
        {
            var state = this.DefaultCountry.AddOrGetState(stateStr);
            var city = state.AddOrGetCity(cityStr);
            var district = city.AddOrGetDistrict(districtStr);
            var neighborhood = district.AddOrGetNeighborhood(neighborhoodStr, postalCodeStr);
        }
    }
}
