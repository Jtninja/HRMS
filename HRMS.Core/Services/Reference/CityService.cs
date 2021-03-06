﻿using HRMS.Core.Common;
using HRMS.Core.Model;
using HRMS.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HRMS.Core.Services
{
    public class CityService : BaseService, ICityService
    {
        private readonly IUniOfWork work;

        public CityService(IUniOfWork work)
        {
            this.work = work;
        }

        async Task<bool> DoesCityExistAsync(string code, int? id)
        {
            var result = id.HasValue ? await work.City.AnyAsync(a => a.Name.ToLower() == code.ToLower() && a.IsValid && a.Id != id)
                : await work.City.AnyAsync(a => a.Name.ToLower() == code.ToLower() && a.IsValid);
            return result;
        }

        public async Task<Response<int>> CreateAsync(City model)
        {
            var result = new Response<int>() { IsSuccessful = true };
            try
            {
                if (await DoesCityExistAsync(model.Name, null))
                {
                    throw new HRMSException("Region already exists");
                }

                if (model.CountryId == 0)
                {
                    throw new HRMSException("Country cant be emty");
                }
                if (model.RegionId == 0)
                {
                    throw new HRMSException("Region cant be emty");
                }
                await work.City.InsertAsync(model);
                await work.SaveChangesAsync();

                var _city = work.City.Where(a => a.Name == model.Name && a.IsValid).FirstOrDefault();

                if (_city == null)
                {
                    throw new HRMSException("City wasnt saved correctly");
                }
                result.Result = _city.Id;
            }
            catch (Exception ex)
            {

                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        public async Task<Response> EditAsync(City model)
        {
            var result = new Response { IsSuccessful = true };
            try
            {
                if (await DoesCityExistAsync(model.Name, model.Id))
                {
                    throw new HRMSException("Region already exists");
                }

                if (model.CountryId == 0)
                {
                    throw new HRMSException("Country cant be emty");
                }
                if (model.RegionId == 0)
                {
                    throw new HRMSException("Region cant be emty");
                }
                var currentEntity = await work.City.GetByIdAsync(model.Id);
                if (currentEntity == null)
                {
                    throw new HRMSException("City cant be saved");
                }
                currentEntity.Name = model.Name;
                currentEntity.CountryId = model.CountryId;
                currentEntity.RegionId = model.RegionId;

                await work.City.UpdateAsync(currentEntity);
                await work.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        public async Task<Response> DeleteAsync(int id)
        {
            var result = new Response { IsSuccessful = true };
            try
            {
                var region = await work.City.GetByIdAsync(id);
                region.IsValid = false;
                await work.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        public async Task<Response<IEnumerable<City>>> GetAllAsync(int? counryId, int? regionId)
        {
            var result = new Response<IEnumerable<City>> { IsSuccessful = true };
            try
            {
                if (regionId.HasValue && regionId.Value > 0)
                {
                    result.Result = await work.City.WhereAsync(a => a.IsValid && a.RegionId == regionId, a => a.Country, a => a.Region);
                }
                else if (counryId.HasValue && counryId.Value > 0)
                {
                    result.Result = await work.City.WhereAsync(a => a.IsValid && a.CountryId == counryId, a => a.Country, a => a.Region);
                }
                else
                {
                    result.Result = await work.City.WhereAsync(a => a.IsValid, a => a.Country, a => a.Region);
                }

            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        public async Task<Response<City>> GetByIdAsync(int id)
        {
            var result = new Response<City> { IsSuccessful = true };
            try
            {
                result.Result = await work.City.FirstOrDefault(a => a.Id == id, a => a.Country, a => a.Region);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }
    }

}
