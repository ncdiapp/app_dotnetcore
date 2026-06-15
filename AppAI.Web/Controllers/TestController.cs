using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using App.BL;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

[ApiController]
[Route("webapi/[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public DataTable GetUser()
    {
        DataAccess dataAccess = new DataAccess();

        string query = "select * from authors";

        DataTable result = dataAccess.ExecuteSelectCommand(query, CommandType.Text);

        result.TableName = "authors";

        return result;

    }


    [HttpPost]
    public OperationCallResult<AppSecurityGroupExDto> SaveAppSecurityGroupExDto(AppSecurityGroupExDto aAppSecurityGroupExDto)
    {
        //startMonitor( "")
        return null;

    }
}


public class DataAccess
{
    private System.Data.SqlClient.SqlConnection con = null;

    public DataAccess()
    {
        con = new SqlConnection(AppConfig.GetConnectionString("TestConnectionString") ?? string.Empty);
    }

    public DataTable ExecuteSelectCommand(string CommandName, CommandType cmdType)
    {
        SqlCommand cmd = null;
        DataTable table = new DataTable();

        cmd = con.CreateCommand();

        cmd.CommandType = cmdType;
        cmd.CommandText = CommandName;

        try
        {
            con.Open();

            SqlDataAdapter da = null;
            using (da = new SqlDataAdapter(cmd))
            {
                da.Fill(table);
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            cmd.Dispose();
            cmd = null;
            con.Close();
        }

        return table;
    }

    public DataTable ExecuteParamerizedSelectCommand(string CommandName, CommandType cmdType, SqlParameter[] param)
    {
        SqlCommand cmd = null;
        DataTable table = new DataTable();

        cmd = con.CreateCommand();

        cmd.CommandType = cmdType;
        cmd.CommandText = CommandName;
        cmd.Parameters.AddRange(param);

        try
        {
            con.Open();

            SqlDataAdapter da = null;
            using (da = new SqlDataAdapter(cmd))
            {
                da.Fill(table);
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            cmd.Dispose();
            cmd = null;
            con.Close();
        }

        return table;
    }

    public bool ExecuteNonQuery(string CommandName, CommandType cmdType, SqlParameter[] pars)
    {
        SqlCommand cmd = null;
        int res = 0;

        cmd = con.CreateCommand();

        cmd.CommandType = cmdType;
        cmd.CommandText = CommandName;
        cmd.Parameters.AddRange(pars);

        try
        {
            con.Open();

            res = cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            cmd.Dispose();
            cmd = null;
            con.Close();
        }

        if (res >= 1)
        {
            return true;
        }
        return false;
    }
}
