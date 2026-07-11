// Core framework usings
global using Microsoft.EntityFrameworkCore;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Rendering;
global using Microsoft.AspNetCore.Mvc.Filters;
global using Microsoft.Extensions.Logging;

// Application-specific usings
global using PharmaCare.Models;
global using PharmaCare.Data;
global using PharmaCare.ViewModels;
global using PharmaCare.Services;
global using PharmaCare.Repositories.Interface;
global using PharmaCare.Repositories.Repository;
global using PharmaCare.Helpers.FileHelper;
global using Repositories.Repository;

// System usings
global using System;
global using System.Linq;
global using System.Threading.Tasks;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;