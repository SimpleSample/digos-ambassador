﻿// <auto-generated />
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.UserInfo;
using DIGOS.Ambassador.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;

namespace DIGOS.Ambassador.Migrations
{
    [DbContext(typeof(GlobalInfoContext))]
    [Migration("20171107143018_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.0-rtm-26452");

            modelBuilder.Entity("DIGOS.Ambassador.Database.Permissions.GlobalPermission", b =>
                {
                    b.Property<uint>("GlobalPermissionID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Permission");

                    b.Property<int>("Target");

                    b.Property<uint?>("UserID");

                    b.HasKey("GlobalPermissionID");

                    b.HasIndex("UserID");

                    b.ToTable("GlobalPermissions");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.Permissions.LocalPermission", b =>
                {
                    b.Property<uint>("LocalPermissionID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Permission");

                    b.Property<uint?>("ServerID");

                    b.Property<int>("Target");

                    b.Property<uint?>("UserID");

                    b.HasKey("LocalPermissionID");

                    b.HasIndex("ServerID");

                    b.HasIndex("UserID");

                    b.ToTable("LocalPermissions");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.Roleplaying.Roleplay", b =>
                {
                    b.Property<uint>("RoleplayID")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ActiveChannelID");

                    b.Property<bool>("IsActive");

                    b.Property<bool>("IsNSFW");

                    b.Property<bool>("IsPublic");

                    b.Property<string>("Name");

                    b.Property<uint?>("OwnerUserID");

                    b.Property<string>("Summary");

                    b.HasKey("RoleplayID");

                    b.HasIndex("OwnerUserID");

                    b.ToTable("Roleplays");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.Roleplaying.UserMessage", b =>
                {
                    b.Property<uint>("UserMessageID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AuthorNickname");

                    b.Property<uint?>("AuthorUserID");

                    b.Property<string>("Contents");

                    b.Property<ulong>("DiscordMessageID");

                    b.Property<uint?>("RoleplayID");

                    b.Property<DateTimeOffset>("Timestamp");

                    b.HasKey("UserMessageID");

                    b.HasIndex("AuthorUserID");

                    b.HasIndex("RoleplayID");

                    b.ToTable("UserMessage");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.ServerInfo.Server", b =>
                {
                    b.Property<uint>("ServerID")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("DiscordGuildID");

                    b.Property<bool>("IsNSFW");

                    b.HasKey("ServerID");

                    b.ToTable("Servers");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.UserInfo.Character", b =>
                {
                    b.Property<uint>("CharacterID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Avatar");

                    b.Property<string>("Description");

                    b.Property<string>("Name");

                    b.Property<string>("Nickname");

                    b.Property<uint?>("OwnerUserID");

                    b.Property<string>("Summary");

                    b.HasKey("CharacterID");

                    b.HasIndex("OwnerUserID");

                    b.ToTable("Characters");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.UserInfo.Kink", b =>
                {
                    b.Property<uint>("KinkID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Category");

                    b.Property<string>("Description");

                    b.Property<uint>("FListID");

                    b.Property<string>("Name");

                    b.HasKey("KinkID");

                    b.ToTable("Kinks");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.UserInfo.User", b =>
                {
                    b.Property<uint>("UserID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Bio");

                    b.Property<int>("Class");

                    b.Property<ulong>("DiscordID");

                    b.Property<uint?>("RoleplayID");

                    b.Property<uint?>("ServerID");

                    b.Property<int?>("Timezone");

                    b.HasKey("UserID");

                    b.HasIndex("RoleplayID");

                    b.HasIndex("ServerID");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.UserInfo.UserKink", b =>
                {
                    b.Property<uint>("UserKinkID")
                        .ValueGeneratedOnAdd();

                    b.Property<uint?>("KinkID");

                    b.Property<int>("Preference");

                    b.Property<uint?>("UserID");

                    b.HasKey("UserKinkID");

                    b.HasIndex("KinkID");

                    b.HasIndex("UserID");

                    b.ToTable("UserKink");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.Permissions.GlobalPermission", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Database.UserInfo.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.Permissions.LocalPermission", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Database.ServerInfo.Server", "Server")
                        .WithMany()
                        .HasForeignKey("ServerID");

                    b.HasOne("DIGOS.Ambassador.Database.UserInfo.User")
                        .WithMany("LocalPermissions")
                        .HasForeignKey("UserID");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.Roleplaying.Roleplay", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Database.UserInfo.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerUserID");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.Roleplaying.UserMessage", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Database.UserInfo.User", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorUserID");

                    b.HasOne("DIGOS.Ambassador.Database.Roleplaying.Roleplay")
                        .WithMany("Messages")
                        .HasForeignKey("RoleplayID");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.UserInfo.Character", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Database.UserInfo.User", "Owner")
                        .WithMany("Characters")
                        .HasForeignKey("OwnerUserID");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.UserInfo.User", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Database.Roleplaying.Roleplay")
                        .WithMany("Participants")
                        .HasForeignKey("RoleplayID");

                    b.HasOne("DIGOS.Ambassador.Database.ServerInfo.Server")
                        .WithMany("KnownUsers")
                        .HasForeignKey("ServerID");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Database.UserInfo.UserKink", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Database.UserInfo.Kink", "Kink")
                        .WithMany()
                        .HasForeignKey("KinkID");

                    b.HasOne("DIGOS.Ambassador.Database.UserInfo.User")
                        .WithMany("Kinks")
                        .HasForeignKey("UserID");
                });
#pragma warning restore 612, 618
        }
    }
}
