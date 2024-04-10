﻿// <auto-generated />
using System;
using BackgroundTaskExecutor.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BackgroundTaskExecutor.Migrations
{
    [DbContext(typeof(BackgroundContext))]
    [Migration("20240410100731_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            modelBuilder.Entity("BackgroundTaskExecutor.Entities.SyncEntry", b =>
                {
                    b.Property<Guid>("Id");

                    b.Property<DateTime>("LastRun");

                    b.Property<string>("MachineName")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.Property<string>("Profile")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.Property<string>("TaskName")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.HasKey("Id");

                    b.ToTable("SyncEntries");
                });
#pragma warning restore 612, 618
        }
    }
}