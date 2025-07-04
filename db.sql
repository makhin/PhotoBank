USE [master]
GO
/****** Object:  Database [Photobank]    Script Date: 30.06.2025 09:07:36 ******/
CREATE DATABASE [Photobank]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'Photobank', FILENAME = N'c:\Databases\Photobank.mdf' , SIZE = 56078272KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'Photobank_log', FILENAME = N'c:\Databases\Photobank_log.ldf' , SIZE = 66658304KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [Photobank] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [Photobank].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [Photobank] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [Photobank] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [Photobank] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [Photobank] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [Photobank] SET ARITHABORT OFF 
GO
ALTER DATABASE [Photobank] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [Photobank] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [Photobank] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [Photobank] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [Photobank] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [Photobank] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [Photobank] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [Photobank] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [Photobank] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [Photobank] SET  DISABLE_BROKER 
GO
ALTER DATABASE [Photobank] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [Photobank] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [Photobank] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [Photobank] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [Photobank] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [Photobank] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [Photobank] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [Photobank] SET RECOVERY FULL 
GO
ALTER DATABASE [Photobank] SET  MULTI_USER 
GO
ALTER DATABASE [Photobank] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [Photobank] SET DB_CHAINING OFF 
GO
ALTER DATABASE [Photobank] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [Photobank] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [Photobank] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [Photobank] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'Photobank', N'ON'
GO
ALTER DATABASE [Photobank] SET QUERY_STORE = OFF
GO
USE [Photobank]
GO
/****** Object:  User [alex]    Script Date: 30.06.2025 09:07:36 ******/
CREATE USER [alex] FOR LOGIN [alex] WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_owner] ADD MEMBER [alex]
GO
/****** Object:  FullTextCatalog [ft]    Script Date: 30.06.2025 09:07:36 ******/
CREATE FULLTEXT CATALOG [ft] WITH ACCENT_SENSITIVITY = ON
AS DEFAULT
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetRoleClaims]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetRoleClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RoleId] [nvarchar](450) NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetRoles]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetRoles](
	[Id] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](256) NULL,
	[NormalizedName] [nvarchar](256) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserClaims]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [nvarchar](450) NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserLogins]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserLogins](
	[LoginProvider] [nvarchar](128) NOT NULL,
	[ProviderKey] [nvarchar](128) NOT NULL,
	[ProviderDisplayName] [nvarchar](max) NULL,
	[UserId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED 
(
	[LoginProvider] ASC,
	[ProviderKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserRoles]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserRoles](
	[UserId] [nvarchar](450) NOT NULL,
	[RoleId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUsers]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUsers](
	[Id] [nvarchar](450) NOT NULL,
	[UserName] [nvarchar](256) NULL,
	[NormalizedUserName] [nvarchar](256) NULL,
	[Email] [nvarchar](256) NULL,
	[NormalizedEmail] [nvarchar](256) NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[PasswordHash] [nvarchar](max) NULL,
	[SecurityStamp] [nvarchar](max) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[LockoutEnd] [datetimeoffset](7) NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
 CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserTokens]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserTokens](
	[UserId] [nvarchar](450) NOT NULL,
	[LoginProvider] [nvarchar](128) NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[LoginProvider] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Captions]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Captions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Text] [nvarchar](max) NULL,
	[Confidence] [float] NOT NULL,
	[PhotoId] [int] NULL,
 CONSTRAINT [PK_Captions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Categories]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Categories](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
 CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Enrichers]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Enrichers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[EnricherType] [int] NOT NULL,
 CONSTRAINT [PK_Enrichers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Faces]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Faces](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Rectangle] [geometry] NULL,
	[Age] [float] NULL,
	[Gender] [bit] NULL,
	[PhotoId] [int] NOT NULL,
	[Image] [varbinary](max) NULL,
	[PersonId] [int] NULL,
	[IdentifiedWithConfidence] [float] NOT NULL,
	[IdentityStatus] [int] NOT NULL,
	[FaceAttributes] [nvarchar](max) NULL,
	[Smile] [float] NULL,
 CONSTRAINT [PK_Faces] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Files]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Files](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[PhotoId] [int] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
 CONSTRAINT [PK_Files] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ObjectProperties]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ObjectProperties](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Rectangle] [geometry] NULL,
	[Confidence] [float] NOT NULL,
	[PhotoId] [int] NULL,
	[PropertyNameId] [int] NULL,
 CONSTRAINT [PK_ObjectProperties] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PersonGroup]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PersonGroup](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_PersonGroup] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PersonGroupFace]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PersonGroupFace](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PersonId] [int] NOT NULL,
	[FaceId] [int] NOT NULL,
	[ExternalGuid] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_PersonGroupFace] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PersonPersonGroup]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PersonPersonGroup](
	[PersonGroupsId] [int] NOT NULL,
	[PersonsId] [int] NOT NULL,
 CONSTRAINT [PK_PersonPersonGroup] PRIMARY KEY CLUSTERED 
(
	[PersonGroupsId] ASC,
	[PersonsId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Persons]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Persons](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[DateOfBirth] [datetime2](7) NULL,
	[ExternalGuid] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_Persons] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PhotoCategories]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PhotoCategories](
	[PhotoId] [int] NOT NULL,
	[CategoryId] [int] NOT NULL,
	[Score] [float] NOT NULL,
 CONSTRAINT [PK_PhotoCategories] PRIMARY KEY CLUSTERED 
(
	[PhotoId] ASC,
	[CategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Photos]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Photos](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[TakenDate] [datetime2](7) NULL,
	[AccentColor] [nvarchar](6) NULL,
	[AdultScore] [float] NOT NULL,
	[DominantColorBackground] [nvarchar](50) NULL,
	[DominantColorForeground] [nvarchar](50) NULL,
	[DominantColors] [nvarchar](150) NULL,
	[Height] [bigint] NULL,
	[IsAdultContent] [bit] NOT NULL,
	[IsBW] [bit] NOT NULL,
	[IsRacyContent] [bit] NOT NULL,
	[Location] [geometry] NULL,
	[Orientation] [int] NULL,
	[PreviewImage] [varbinary](max) NULL,
	[RacyScore] [float] NOT NULL,
	[Width] [bigint] NULL,
	[RelativePath] [nvarchar](255) NULL,
	[StorageId] [int] NOT NULL,
	[Thumbnail] [varbinary](max) NULL,
	[Scale] [float] NOT NULL,
	[FaceIdentifyStatus] [int] NOT NULL,
	[EnrichedWithEnricherType] [int] NOT NULL,
 CONSTRAINT [PK_Photos] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PhotoTags]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PhotoTags](
	[PhotoId] [int] NOT NULL,
	[TagId] [int] NOT NULL,
	[Confidence] [float] NOT NULL,
 CONSTRAINT [PK_PhotoTags] PRIMARY KEY CLUSTERED 
(
	[PhotoId] ASC,
	[TagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PropertyNames]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PropertyNames](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
 CONSTRAINT [PK_PropertyNames] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Storages]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Storages](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[Folder] [nvarchar](max) NULL,
 CONSTRAINT [PK_Storages] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tags]    Script Date: 30.06.2025 09:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tags](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[Hint] [nvarchar](max) NULL,
 CONSTRAINT [PK_Tags] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_AspNetRoleClaims_RoleId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [RoleNameIndex]    Script Date: 30.06.2025 09:07:36 ******/
CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex] ON [dbo].[AspNetRoles]
(
	[NormalizedName] ASC
)
WHERE ([NormalizedName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_AspNetUserClaims_UserId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_AspNetUserLogins_UserId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_AspNetUserRoles_RoleId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [EmailIndex]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [EmailIndex] ON [dbo].[AspNetUsers]
(
	[NormalizedEmail] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UserNameIndex]    Script Date: 30.06.2025 09:07:36 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] ON [dbo].[AspNetUsers]
(
	[NormalizedUserName] ASC
)
WHERE ([NormalizedUserName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Captions_PhotoId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Captions_PhotoId] ON [dbo].[Captions]
(
	[PhotoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UI_Captions_Id]    Script Date: 30.06.2025 09:07:36 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UI_Captions_Id] ON [dbo].[Captions]
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Enrichers_Name]    Script Date: 30.06.2025 09:07:36 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Enrichers_Name] ON [dbo].[Enrichers]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Faces_IdentityStatus]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Faces_IdentityStatus] ON [dbo].[Faces]
(
	[IdentityStatus] ASC
)
INCLUDE([PersonId]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Faces_PersonId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Faces_PersonId] ON [dbo].[Faces]
(
	[PersonId] ASC
)
INCLUDE([PhotoId]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Faces_PhotoId_Id_PersonId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Faces_PhotoId_Id_PersonId] ON [dbo].[Faces]
(
	[PhotoId] ASC,
	[Id] ASC,
	[PersonId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Files_Name]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Files_Name] ON [dbo].[Files]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Files_Name_PhotoId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Files_Name_PhotoId] ON [dbo].[Files]
(
	[Name] ASC,
	[PhotoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Files_PhotoId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Files_PhotoId] ON [dbo].[Files]
(
	[PhotoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ObjectProperties_PhotoId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_ObjectProperties_PhotoId] ON [dbo].[ObjectProperties]
(
	[PhotoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ObjectProperties_PropertyNameId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_ObjectProperties_PropertyNameId] ON [dbo].[ObjectProperties]
(
	[PropertyNameId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PersonGroupFace_FaceId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_PersonGroupFace_FaceId] ON [dbo].[PersonGroupFace]
(
	[FaceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PersonGroupFace_PersonId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_PersonGroupFace_PersonId] ON [dbo].[PersonGroupFace]
(
	[PersonId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PersonPersonGroup_PersonsId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_PersonPersonGroup_PersonsId] ON [dbo].[PersonPersonGroup]
(
	[PersonsId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PhotoCategories_CategoryId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_PhotoCategories_CategoryId] ON [dbo].[PhotoCategories]
(
	[CategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Photos_Id]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Photos_Id] ON [dbo].[Photos]
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Photos_IsAdultContent]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Photos_IsAdultContent] ON [dbo].[Photos]
(
	[IsAdultContent] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Photos_IsBW]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Photos_IsBW] ON [dbo].[Photos]
(
	[IsBW] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Photos_IsRacyContent]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Photos_IsRacyContent] ON [dbo].[Photos]
(
	[IsRacyContent] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Photos_Name_RelativePath]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Photos_Name_RelativePath] ON [dbo].[Photos]
(
	[Name] ASC,
	[RelativePath] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Photos_StorageId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Photos_StorageId] ON [dbo].[Photos]
(
	[StorageId] ASC
)
INCLUDE([RelativePath]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Photos_TakenDate]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_Photos_TakenDate] ON [dbo].[Photos]
(
	[TakenDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PhotoTags_PhotoId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_PhotoTags_PhotoId] ON [dbo].[PhotoTags]
(
	[PhotoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PhotoTags_TagId]    Script Date: 30.06.2025 09:07:36 ******/
CREATE NONCLUSTERED INDEX [IX_PhotoTags_TagId] ON [dbo].[PhotoTags]
(
	[TagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Enrichers] ADD  DEFAULT (N'') FOR [Name]
GO
ALTER TABLE [dbo].[Enrichers] ADD  DEFAULT ((0)) FOR [EnricherType]
GO
ALTER TABLE [dbo].[Faces] ADD  DEFAULT ((0)) FOR [PhotoId]
GO
ALTER TABLE [dbo].[Faces] ADD  DEFAULT ((0.0000000000000000e+000)) FOR [IdentifiedWithConfidence]
GO
ALTER TABLE [dbo].[Faces] ADD  DEFAULT ((0)) FOR [IdentityStatus]
GO
ALTER TABLE [dbo].[Files] ADD  DEFAULT ((0)) FOR [PhotoId]
GO
ALTER TABLE [dbo].[Files] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[Persons] ADD  DEFAULT ('00000000-0000-0000-0000-000000000000') FOR [ExternalGuid]
GO
ALTER TABLE [dbo].[Photos] ADD  DEFAULT ((0.0000000000000000e+000)) FOR [AdultScore]
GO
ALTER TABLE [dbo].[Photos] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsAdultContent]
GO
ALTER TABLE [dbo].[Photos] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsBW]
GO
ALTER TABLE [dbo].[Photos] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsRacyContent]
GO
ALTER TABLE [dbo].[Photos] ADD  DEFAULT ((0.0000000000000000e+000)) FOR [RacyScore]
GO
ALTER TABLE [dbo].[Photos] ADD  DEFAULT ((0)) FOR [StorageId]
GO
ALTER TABLE [dbo].[Photos] ADD  DEFAULT ((0.0000000000000000e+000)) FOR [Scale]
GO
ALTER TABLE [dbo].[Photos] ADD  DEFAULT ((0)) FOR [FaceIdentifyStatus]
GO
ALTER TABLE [dbo].[Photos] ADD  DEFAULT ((0)) FOR [EnrichedWithEnricherType]
GO
ALTER TABLE [dbo].[AspNetRoleClaims]  WITH CHECK ADD  CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AspNetRoles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetRoleClaims] CHECK CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId]
GO
ALTER TABLE [dbo].[AspNetUserClaims]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserClaims] CHECK CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[AspNetUserLogins]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserLogins] CHECK CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AspNetRoles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId]
GO
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[AspNetUserTokens]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserTokens] CHECK CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[Captions]  WITH CHECK ADD  CONSTRAINT [FK_Captions_Photos_PhotoId] FOREIGN KEY([PhotoId])
REFERENCES [dbo].[Photos] ([Id])
GO
ALTER TABLE [dbo].[Captions] CHECK CONSTRAINT [FK_Captions_Photos_PhotoId]
GO
ALTER TABLE [dbo].[Faces]  WITH CHECK ADD  CONSTRAINT [FK_Faces_Persons_PersonId] FOREIGN KEY([PersonId])
REFERENCES [dbo].[Persons] ([Id])
GO
ALTER TABLE [dbo].[Faces] CHECK CONSTRAINT [FK_Faces_Persons_PersonId]
GO
ALTER TABLE [dbo].[Faces]  WITH CHECK ADD  CONSTRAINT [FK_Faces_Photos_PhotoId] FOREIGN KEY([PhotoId])
REFERENCES [dbo].[Photos] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Faces] CHECK CONSTRAINT [FK_Faces_Photos_PhotoId]
GO
ALTER TABLE [dbo].[Files]  WITH CHECK ADD  CONSTRAINT [FK_Files_Photos_PhotoId] FOREIGN KEY([PhotoId])
REFERENCES [dbo].[Photos] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Files] CHECK CONSTRAINT [FK_Files_Photos_PhotoId]
GO
ALTER TABLE [dbo].[ObjectProperties]  WITH CHECK ADD  CONSTRAINT [FK_ObjectProperties_Photos_PhotoId] FOREIGN KEY([PhotoId])
REFERENCES [dbo].[Photos] ([Id])
GO
ALTER TABLE [dbo].[ObjectProperties] CHECK CONSTRAINT [FK_ObjectProperties_Photos_PhotoId]
GO
ALTER TABLE [dbo].[ObjectProperties]  WITH CHECK ADD  CONSTRAINT [FK_ObjectProperties_PropertyNames_PropertyNameId] FOREIGN KEY([PropertyNameId])
REFERENCES [dbo].[PropertyNames] ([Id])
GO
ALTER TABLE [dbo].[ObjectProperties] CHECK CONSTRAINT [FK_ObjectProperties_PropertyNames_PropertyNameId]
GO
ALTER TABLE [dbo].[PersonGroupFace]  WITH CHECK ADD  CONSTRAINT [FK_PersonGroupFace_Faces_FaceId] FOREIGN KEY([FaceId])
REFERENCES [dbo].[Faces] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PersonGroupFace] CHECK CONSTRAINT [FK_PersonGroupFace_Faces_FaceId]
GO
ALTER TABLE [dbo].[PersonGroupFace]  WITH CHECK ADD  CONSTRAINT [FK_PersonGroupFace_Persons_PersonId] FOREIGN KEY([PersonId])
REFERENCES [dbo].[Persons] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PersonGroupFace] CHECK CONSTRAINT [FK_PersonGroupFace_Persons_PersonId]
GO
ALTER TABLE [dbo].[PersonPersonGroup]  WITH CHECK ADD  CONSTRAINT [FK_PersonPersonGroup_PersonGroup_PersonGroupsId] FOREIGN KEY([PersonGroupsId])
REFERENCES [dbo].[PersonGroup] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PersonPersonGroup] CHECK CONSTRAINT [FK_PersonPersonGroup_PersonGroup_PersonGroupsId]
GO
ALTER TABLE [dbo].[PersonPersonGroup]  WITH CHECK ADD  CONSTRAINT [FK_PersonPersonGroup_Persons_PersonsId] FOREIGN KEY([PersonsId])
REFERENCES [dbo].[Persons] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PersonPersonGroup] CHECK CONSTRAINT [FK_PersonPersonGroup_Persons_PersonsId]
GO
ALTER TABLE [dbo].[PhotoCategories]  WITH CHECK ADD  CONSTRAINT [FK_PhotoCategories_Categories_CategoryId] FOREIGN KEY([CategoryId])
REFERENCES [dbo].[Categories] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PhotoCategories] CHECK CONSTRAINT [FK_PhotoCategories_Categories_CategoryId]
GO
ALTER TABLE [dbo].[PhotoCategories]  WITH CHECK ADD  CONSTRAINT [FK_PhotoCategories_Photos_PhotoId] FOREIGN KEY([PhotoId])
REFERENCES [dbo].[Photos] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PhotoCategories] CHECK CONSTRAINT [FK_PhotoCategories_Photos_PhotoId]
GO
ALTER TABLE [dbo].[Photos]  WITH CHECK ADD  CONSTRAINT [FK_Photos_Storages_StorageId] FOREIGN KEY([StorageId])
REFERENCES [dbo].[Storages] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Photos] CHECK CONSTRAINT [FK_Photos_Storages_StorageId]
GO
ALTER TABLE [dbo].[PhotoTags]  WITH CHECK ADD  CONSTRAINT [FK_PhotoTags_Photos_PhotoId] FOREIGN KEY([PhotoId])
REFERENCES [dbo].[Photos] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PhotoTags] CHECK CONSTRAINT [FK_PhotoTags_Photos_PhotoId]
GO
ALTER TABLE [dbo].[PhotoTags]  WITH CHECK ADD  CONSTRAINT [FK_PhotoTags_Tags_TagId] FOREIGN KEY([TagId])
REFERENCES [dbo].[Tags] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PhotoTags] CHECK CONSTRAINT [FK_PhotoTags_Tags_TagId]
GO
USE [master]
GO
ALTER DATABASE [Photobank] SET  READ_WRITE 
GO
