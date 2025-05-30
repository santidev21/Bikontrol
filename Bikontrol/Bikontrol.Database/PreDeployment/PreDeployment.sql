IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'BikontrolDb')
BEGIN
    CREATE DATABASE [BikontrolDb];
END
GO

USE [BikontrolDb];
GO