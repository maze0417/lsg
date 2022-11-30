﻿// <auto-generated />
using System;
using LSG.Infrastructure.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace LSG.Infrastructure.DataServices.Migrations
{
    [DbContext(typeof(LsgRepository))]
    [Migration("20221128090425_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("LSG.Core.Entities.Schema", b =>
                {
                    b.Property<long>("Version")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("Version");

                    b.ToTable("Schemas", (string)null);
                });

            modelBuilder.Entity("LSG.Core.Messages.StoredProc.GetSchema", b =>
                {
                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("Version")
                        .HasColumnType("bigint");

                    b.ToTable((string)null);

                    b.ToView("GetSchema", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}