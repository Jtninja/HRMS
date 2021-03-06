﻿using HRMS.Core.Common;
using HRMS.Core.Model;
using HRMS.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HRMS.Core.Services
{
    public class OrganigramService : BaseService, IOrganigramService
    {
        private readonly IUniOfWork work;

        public OrganigramService(IUniOfWork work)
        {
            this.work = work;
        }

        public async Task IsModelValid(Organigram model, bool checkId = false)
        {
            if (model.CompanyId == Guid.Empty || string.IsNullOrEmpty(model.Name))
            {
                throw new HRMSException("Please enter correct info(Name,Company)");
            }
            if (model.RespondsToId == Guid.Empty)
            {
                model.RespondsToId = null;
            }
            if (model.CompanyDepartamentId == Guid.Empty)
            {
                model.CompanyDepartamentId = null;
            }
            if (model.CompanyDepartamentId.HasValue && !model.IsCeo)
            {
                throw new HRMSException("This position should have a boss (respondsTo)");
            }

            //Check if the name exists in the company and companySite
            bool nameExits = false;
            if (checkId)
            {

                nameExits = await work.Organigram.AnyAsync(a =>
                                                            a.Name.ToLower() == model.Name.ToLower() &&
                                                            a.CompanyId == model.CompanyId &&
                                                            a.CompanyDepartamentId == model.CompanyDepartamentId &&
                                                            a.IsValid &&
                                                            a.Id != model.Id
                                                           );


            }
            else
            {
                nameExits = await work.Organigram.AnyAsync(a =>
                                                           a.Name.ToLower() == model.Name.ToLower() &&
                                                           a.CompanyId == model.CompanyId &&
                                                           a.CompanyDepartamentId == model.CompanyDepartamentId &&
                                                           a.IsValid
                                                          );
            }
            if (nameExits)
            {
                throw new HRMSException("Position already exists");
            }

        }

        public async Task<Response> CreateAsync(Organigram model)
        {
            var result = new Response() { IsSuccessful = true };
            try
            {
                model.Id = Guid.NewGuid();
                await IsModelValid(model);

                await work.Organigram.InsertAsync(model);
                await work.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        public async Task<Response> EditAsync(Organigram model)
        {
            var result = new Response { IsSuccessful = true };
            try
            {
                await IsModelValid(model, true);
                var currentEntity = await work.Organigram.GetByIdAsync(model.Id);
                if (currentEntity == null)
                {
                    throw new HRMSException("Organigram cant be saved,entity couldnt be found");
                }
                currentEntity.Name = model.Name;
                currentEntity.IsCeo = model.IsCeo;
                currentEntity.CompanyDepartamentId = model.CompanyDepartamentId;
                currentEntity.RespondsToId = model.RespondsToId;


                await work.Organigram.UpdateAsync(currentEntity);
                await work.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        public async Task<Response> DeleteAsync(Guid id)
        {
            var result = new Response { IsSuccessful = true };
            try
            {
                var currentEntity = await work.Organigram.GetByIdAsync(id);
                currentEntity.IsValid = false;
                await work.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        public async Task<Response<IEnumerable<Organigram>>> GetAllAsync(Guid? companyId)
        {
            var result = new Response<IEnumerable<Organigram>> { IsSuccessful = true };
            try
            {
                if (companyId.HasValue)
                {
                    result.Result = await work.Organigram.WhereAsync(
                                                                        a => a.IsValid && a.CompanyId == companyId,
                                                                        a => a.RespondsTo,
                                                                        a => a.CompanyDepartament,
                                                                        a => a.CompanyDepartament.Departament, 
                                                                        a => a.Company);
                }
                else
                {
                    result.Result = await work.Organigram.WhereAsync(
                                                                        a => a.IsValid,
                                                                        a => a.RespondsTo,
                                                                        a => a.CompanyDepartament, 
                                                                        a => a.CompanyDepartament.Departament,
                                                                        a => a.Company
                                                                        );
                }

            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        public async Task<Response<Organigram>> GetByIdAsync(Guid id)
        {
            var result = new Response<Organigram> { IsSuccessful = true };
            try
            {
                result.Result = await work.Organigram.FirstOrDefault(a => a.Id == id,
                    a => a.RespondsTo, a => a.CompanyDepartament, a => a.CompanyDepartament.Departament, a => a.Company);

            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        public async Task<Response<OrganigramEmployee>> GetCurrentEmployeeDetailsForOrganigram(Guid organigramId, DateTime? currentDate)
        {

            var result = new Response<OrganigramEmployee> { IsSuccessful = true };
            try
            {
                var data = currentDate.HasValue ?
                     await work.OrganigramEmployee.FirstOrDefault(a => a.IsValid && a.OrganigramId == organigramId && !a.EndDate.HasValue, a => a.Organigram, a => a.Employee) :
                     await work.OrganigramEmployee.FirstOrDefault(a => a.IsValid && a.OrganigramId == organigramId && a.EndDate > currentDate, a => a.Organigram, a => a.Employee);

                result.Result = data;

            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }
        public async Task<Response<OrganigramEmployee>> GetCurrentEmployeeDetails(Guid id)
        {

            var result = new Response<OrganigramEmployee> { IsSuccessful = true };
            try
            {
                var data =
                     await work.OrganigramEmployee.FirstOrDefault(a => a.IsValid && a.Id == id, a => a.Organigram, a => a.Employee);
                result.Result = data;

            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        public async Task<Response<IEnumerable<OrganigramEmployee>>> GetOrganigramEmployeHistory(Guid organigramId)
        {
            var result = new Response<IEnumerable<OrganigramEmployee>> { IsSuccessful = true };
            try
            {
                var datas =
                     await work.OrganigramEmployee.WhereAsync(a => a.IsValid && a.OrganigramId == organigramId, a => a.Organigram, a => a.Employee);

                result.Result = datas;

            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        public async Task<Response> AddEmployee(OrganigramEmployee model)
        {
            var result = new Response { IsSuccessful = true };
            try
            {
                model.Id = Guid.NewGuid();
                await IsModelVAlid(model);
                await work.OrganigramEmployee.InsertAsync(model);
                await work.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        public async Task<Response> EditEmployee(OrganigramEmployee model)
        {
            var result = new Response { IsSuccessful = true };
            try
            {
                await IsModelVAlid(model, true);
                var currentEntity = await work.OrganigramEmployee.GetByIdAsync(model.Id);

                currentEntity.IsValid = true;
                currentEntity.EmployeeId = model.EmployeeId;
                currentEntity.OrganigramId = model.OrganigramId;
                currentEntity.BrutoAmountInMonth = model.BrutoAmountInMonth;
                currentEntity.NetAmountInMonth = model.NetAmountInMonth;
                currentEntity.StartDate = model.StartDate;
                currentEntity.EndDate = model.EndDate;

                await work.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                result.Exception = ex;
                result.IsSuccessful = false;
            }
            return result;
        }

        private async Task IsModelVAlid(OrganigramEmployee model, bool checkId = false)
        {
            if (checkId && model.Id == Guid.Empty)
            {
                throw new HRMSException("Invalid id provided");
            }
            if (model.OrganigramId == Guid.Empty)
            {
                throw new HRMSException("Invalid Employee, please select an organigram");
            }
            if (model.EmployeeId == Guid.Empty)
            {
                throw new HRMSException("Invalid Employee, please select an employee");
            }
            if (model.StartDate == DateTime.MinValue)
            {
                throw new HRMSException("Invalid employee details, please provide start-Date");
            }
            if (model.BrutoAmountInMonth == 0)
            {
                throw new HRMSException("Invalid employee details, please provide Bruto-Amount");
            }
            //TD
            var organigramEmployess = await work.OrganigramEmployee.WhereAsync(a => a.OrganigramId == model.OrganigramId && a.IsValid);

            if (checkId)
            {
                organigramEmployess = organigramEmployess.Where(a => a.Id != model.Id).ToList();
            }
            if (organigramEmployess.Count > 0 && organigramEmployess.Any(a => a.EndDate > model.StartDate))
            {
                throw new HRMSException("Invalid employee details, we already have a employee in this position");
            }
        }

        public async Task<Response> DeleteEmployeeDetail(Guid id)
        {
            var result = new Response { IsSuccessful = true };
            try
            {
                var currentEntity = await work.OrganigramEmployee.GetByIdAsync(id);
                currentEntity.IsValid = false;
                await work.SaveChangesAsync();
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
