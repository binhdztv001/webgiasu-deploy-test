# 🎓 Learn with Mentor - Hệ thống Học tập trực tuyến

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-green)](https://docs.microsoft.com/aspnet/core/)
[![License](https://img.shields.io/badge/license-MIT-orange)](LICENSE)

## 📋 Mục lục
- [Giới thiệu](#giới-thiệu)
- [Công nghệ sử dụng](#công-nghệ-sử-dụng)
- [Kiến trúc hệ thống](#kiến-trúc-hệ-thống)
- [Tính năng chính](#tính-năng-chính)
- [Cài đặt và chạy](#cài-đặt-và-chạy)
- [Cấu trúc dự án](#cấu-trúc-dự-án)
- [Database Schema](#database-schema)
- [API Documentation](#api-documentation)
- [Screenshots](#screenshots)
- [Đóng góp](#đóng-góp)
- [License](#license)

## 🌟 Giới thiệu

**Learn with Mentor** là một nền tảng kết nối học sinh và người hướng dẫn (Mentor) trực tuyến, được xây dựng bằng ASP.NET Core 8.0 MVC. Hệ thống cung cấp một giải pháp toàn diện cho việc quản lý bài tập, lời giải, thanh toán và tương tác giữa học sinh và Mentor.

### ✨ Điểm nổi bật
- 🎯 **4 vai trò người dùng**: Student (Học sinh), Mentor (Người hướng dẫn), Admin (Quản trị), School (Trường học)
- 💬 **Hệ thống chat realtime** với SignalR
- 👥 **Mạng xã hội cộng đồng** với posts, comments, reactions
- 🤖 **AI Assistant** tích hợp Google Gemini AI
- 💳 **Thanh toán trực tuyến** qua SePay Gateway
- 👨‍👩‍👧‍👦 **Nhóm học tập** chia sẻ chi phí
- 👤 **Bạn bè & tin nhắn** giữa người dùng
- ⭐ **Hệ thống đánh giá** Mentor
- 📊 **Thống kê chi tiết** cho từng vai trò
- 💎 **Premium membership** với các tính năng đặc biệt

## 🛠 Công nghệ sử dụng

### Backend
- **Framework**: ASP.NET Core 8.0 MVC
- **ORM**: Entity Framework Core 8.0.6
- **Database**: SQL Server
- **Real-time**: SignalR
- **AI**: Google Gemini 2.5 Flash
- **Payment**: SePay Gateway
- **API Documentation**: Swagger/OpenAPI

### Frontend
- **UI Framework**: Bootstrap 5
- **Icons**: Bootstrap Icons
- **JavaScript**: Vanilla JS + SignalR Client
- **CSS**: Custom responsive design

### Architecture
- **Pattern**: MVC (Model-View-Controller)
- **Service Layer**: Dependency Injection
- **Authentication**: Session-based
- **Authorization**: Role-based access control

## 🏗 Kiến trúc hệ thống

```
┌─────────────────────────────────────────────────────────────┐
│                        Presentation Layer                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  Views   │  │ Layouts  │  │ Partials │  │Components│   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                      Controller Layer                        │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ Student  │  │  Tutor   │  │  Admin   │  │  School  │   │
│  │Controller│  │Controller│  │Controller│  │Controller│   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                       Service Layer                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  User    │  │ Problem  │  │ Payment  │  │Community │   │
│  │ Service  │  │ Service  │  │ Service  │  │ Service  │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                      Data Access Layer                       │
│  ┌──────────────────────────────────────────────────────┐  │
│  │             Entity Framework Core DbContext          │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                       SQL Server Database                    │
└─────────────────────────────────────────────────────────────┘
```

## 🎯 Tính năng chính

### 👨‍🎓 Học sinh (Student)
#### Quản lý bài tập
- ✅ Đăng bài toán cần giải (với title, description, môn học, độ khó, deadline, file đính kèm)
- ✅ Tạo nhóm học tập để chia sẻ chi phí
- ✅ Mời bạn bè tham gia nhóm
- ✅ Xem danh sách bài toán cá nhân và nhóm
- ✅ Chỉnh sửa/xóa bài toán chưa có Mentor nhận
- ✅ Xem chi tiết lời giải từ Mentor
- ✅ Đánh giá Mentor (rating & feedback)

#### Thanh toán
- ✅ Thanh toán qua SePay Gateway
- ✅ Thanh toán cá nhân
- ✅ Thanh toán nhóm (chia đều chi phí)
- ✅ Lịch sử thanh toán chi tiết

#### Mạng xã hội
- ✅ Kết bạn với học sinh/Mentor khác
- ✅ Chat realtime với bạn bè
- ✅ Tham gia cộng đồng: đăng bài, bình luận
- ✅ Reaction (Like, Love, Haha, Angry) cho posts/comments
- ✅ Anonymous posting

#### Premium Features
- ✅ Nâng cấp tài khoản Premium
- ✅ Xem thông tin Mentor được phân công
- ✅ Các tính năng đặc biệt

#### AI Assistant
- ✅ Chat với AI Gemini để được hỗ trợ giải bài
- ✅ Lịch sử chat AI

#### Thống kê & báo cáo
- 📊 Dashboard cá nhân với biểu đồ
- 📊 Thống kê bài toán theo môn học, độ khó
- 📊 Thống kê chi tiêu theo tháng

### 👨‍🏫 Mentor (Người hướng dẫn)
#### Quản lý công việc
- ✅ Xem danh sách bài toán có sẵn
- ✅ Nhận bài toán (First-come-first-served)
- ✅ Xem danh sách bài đã nhận
- ✅ Gửi lời giải (text + file đính kèm)
- ✅ Lịch sử lời giải đã gửi

#### Hồ sơ Mentor
- ✅ Cập nhật thông tin cá nhân
- ✅ Bio, môn dạy, trình độ, kinh nghiệm, chứng chỉ
- ✅ Xem đánh giá từ học sinh

#### Mạng xã hội
- ✅ Kết bạn với Mentor/học sinh khác
- ✅ Chat realtime
- ✅ Cộng đồng Mentor
- ✅ Chia sẻ kinh nghiệm

#### Thu nhập
- 💰 Xem tổng thu nhập
- 💰 Thu nhập theo tháng
- 💰 Danh sách bài đã giải và thu nhập

#### Premium Features
- ✅ Nâng cấp tài khoản Premium
- ✅ Các tính năng đặc biệt

#### Thống kê
- 📊 Dashboard với biểu đồ
- 📊 Số bài đã giải
- 📊 Thu nhập theo thời gian
- 📊 Đánh giá trung bình

### 👨‍💼 Admin (Quản trị viên)
#### Quản lý người dùng
- ✅ Xem danh sách tất cả người dùng
- ✅ Duyệt tài khoản Mentor mới
- ✅ Quản lý tài khoản Student/Mentor

#### Quản lý bài toán
- ✅ Xem tất cả bài toán
- ✅ Xem chi tiết bài toán
- ✅ Xóa bài toán vi phạm

#### Quản lý thanh toán
- ✅ Xem tất cả giao dịch
- ✅ Thống kê doanh thu

#### Thống kê & báo cáo
- 📊 Dashboard tổng quan
- 📊 Số lượng người dùng
- 📊 Số bài toán theo trạng thái
- 📊 Doanh thu theo tháng
- 📊 Top Mentor xuất sắc
- 📊 Thống kê theo môn học

### 🏫 School (Trường học)
#### Quản lý lớp học
- ✅ Tạo lớp học mới
- ✅ Quản lý danh sách lớp
- ✅ Xem chi tiết lớp học
- ✅ Chỉnh sửa thông tin lớp
- ✅ Xóa lớp học

#### Thống kê trường học
- 📊 Dashboard trường học
- 📊 Thống kê lớp học
- 📊 Báo cáo hoạt động

#### Cài đặt
- ✅ Cấu hình trường học
- ✅ Quản lý thông tin

## 💻 Cài đặt và chạy

### Yêu cầu hệ thống
- **.NET 8.0 SDK** hoặc cao hơn
- **SQL Server 2019** hoặc cao hơn (hoặc SQL Server Express)
- **Visual Studio 2022** hoặc **VS Code**
- **Node.js** (optional, cho tooling)

### Cài đặt

1. **Clone repository**
```bash
git clone https://github.com/Toan-Huynh2054/NghienCuuKhoaHoc-AppGiaSu.git
cd NghienCuuKhoaHoc-AppGiaSu
```

2. **Cấu hình Database**

Mở file `appsettings.json` và cập nhật connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=WebgiasuDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

3. **Cấu hình API Keys**

Cập nhật các API keys trong `appsettings.json`:
```json
{
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "Model": "gemini-2.5-flash"
  },
  "SePay": {
    "Env": "production",
    "MerchantId": "YOUR_MERCHANT_ID",
    "SecretKey": "YOUR_SECRET_KEY",
    "ApiBaseUrl": "https://pay-sandbox.sepay.vn",
    "CheckoutPath": "/v1/checkout/init"
  }
}
```

4. **Restore NuGet packages**
```bash
dotnet restore
```

5. **Chạy migrations** (tạo database)
```bash
dotnet ef database update
```

Nếu chưa có migration, tạo mới:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

6. **Build project**
```bash
dotnet build
```

7. **Chạy ứng dụng**
```bash
dotnet run
```

8. **Mở trình duyệt**
```
https://localhost:7xxx
hoặc
http://localhost:5xxx
```

### Swagger API Documentation

Khi chạy ở Development mode, truy cập:
```
https://localhost:7xxx/swagger
```

## 📁 Cấu trúc dự án

```
Webgiasu/
├── Controllers/              # MVC Controllers
│   ├── HomeController.cs
│   ├── AccountController.cs
│   ├── StudentController.cs
│   ├── TutorController.cs
│   ├── AdminController.cs
│   ├── SchoolController.cs
│   ├── AiController.cs
│   ├── PremiumController.cs
│   └── SePayController.cs
│
├── Models/                   # Domain Models
│   ├── User.cs
│   ├── Problem.cs
│   ├── Solution.cs
│   ├── Payment.cs
│   ├── GroupPayment.cs
│   ├── Rating.cs
│   ├── Friendship.cs
│   ├── Message.cs
│   ├── CommunityPost.cs
│   ├── CommunityComment.cs
│   ├── ProblemGroups.cs
│   ├── SchoolClass.cs
│   ├── AppDbContext.cs
│   └── ViewModels/           # View Models
│       ├── AuthViewModel.cs
│       ├── ProblemViewModel.cs
│       ├── RatingViewModel.cs
│       ├── ClassViewModel.cs
│       └── ...
│
├── Services/                 # Business Logic Layer
│   ├── IUserService.cs / UserService.cs
│   ├── IProblemService.cs / ProblemService.cs
│   ├── ISolutionService.cs / SolutionService.cs
│   ├── IPaymentService.cs / PaymentService.cs
│   ├── IRatingService.cs / RatingService.cs
│   ├── IFriendshipService.cs / FriendshipService.cs
│   ├── IMessageService.cs / MessageService.cs
│   ├── ICommunityService.cs / CommunityService.cs
│   ├── IProblemGroupService.cs / ProblemGroupService.cs
│   ├── IPremiumService.cs / PremiumService.cs
│   ├── ISchoolClassService.cs / SchoolClassService.cs
│   ├── GeminiService.cs
│   └── SePayGateway.cs
│
├── Views/                    # Razor Views
│   ├── Home/
│   ├── Account/
│   ├── Student/
│   ├── Tutor/
│   ├── Admin/
│   ├── School/
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _StudentLayout.cshtml
│   │   ├── _TutorLayout.cshtml
│   │   ├── _AdminLayout.cshtml
│   │   ├── _SchoolLayout.cshtml
│   │   └── _AiChatPartial.cshtml
│   └── Components/
│       └── MessageDropdown/
│
├── ViewComponents/           # View Components
│   └── MessageDropdownViewComponent.cs
│
├── Hubs/                     # SignalR Hubs
│   ├── ChatHub.cs
│   └── CommunityHub.cs
│
├── wwwroot/                  # Static Files
│   ├── css/
│   │   ├── site.css
│   │   ├── home.css
│   │   ├── student-layout.css
│   │   ├── tutor-layout.css
│   │   ├── admin-layout.css
│   │   └── school-layout.css
│   ├── js/
│   ├── images/
│   └── files/
│
├── Migrations/               # EF Core Migrations
│   ├── 20251225064131_NewDB.cs
│   ├── 20251225155250_AddPremiumFieldsToUser.cs
│   └── 20251225161227_newGroupPayments.cs
│
├── Program.cs                # Application Entry Point
├── appsettings.json          # Configuration
└── Webgiasu.csproj           # Project File
```

## 🗄 Database Schema

### Core Tables

#### Users
```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    Email NVARCHAR(200),
    PhoneNumber NVARCHAR(20),
    Role INT NOT NULL, -- 0: Student, 1: Tutor, 2: Admin, 3: School
    IsApproved BIT DEFAULT 0,
    RegisteredDate DATETIME DEFAULT GETDATE(),
    IsPremium BIT DEFAULT 0,
    PremiumExpiredAt DATETIME NULL,
    -- Tutor Profile Fields
    Bio NVARCHAR(MAX),
    Subjects NVARCHAR(500),
    Education NVARCHAR(500),
    ExperienceYears INT,
    Certificates NVARCHAR(500)
);
```

#### Problems
```sql
CREATE TABLE Problems (
    Id INT PRIMARY KEY IDENTITY,
    StudentId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Title NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX),
    Type INT NOT NULL, -- 0: Math, 1: Physics, 2: Chemistry, etc.
    Difficulty INT NOT NULL, -- 0: Easy, 1: Medium, 2: Hard
    ImageUrl NVARCHAR(500),
    AttachmentFile NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETDATE(),
    Deadline DATETIME NOT NULL,
    Status INT DEFAULT 0, -- 0: Waiting, 1: InProgress, 2: Solved
    AssignedTutorId INT NULL FOREIGN KEY REFERENCES Users(Id),
    Price DECIMAL(18,2) NOT NULL
);
```

#### Solutions
```sql
CREATE TABLE Solutions (
    Id INT PRIMARY KEY IDENTITY,
    ProblemId INT NOT NULL FOREIGN KEY REFERENCES Problems(Id),
    TutorId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Content NVARCHAR(MAX) NOT NULL,
    FileUrl NVARCHAR(500),
    SubmittedDate DATETIME DEFAULT GETDATE()
);
```

#### Payments
```sql
CREATE TABLE Payments (
    Id INT PRIMARY KEY IDENTITY,
    StudentId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    ProblemId INT NOT NULL FOREIGN KEY REFERENCES Problems(Id),
    Amount DECIMAL(18,2) NOT NULL,
    Status INT DEFAULT 0, -- 0: Pending, 1: Completed, 2: Failed
    CreatedDate DATETIME DEFAULT GETDATE(),
    CompletedDate DATETIME NULL,
    TransactionId NVARCHAR(200)
);
```

### Social Features

#### Friendships
```sql
CREATE TABLE Friendships (
    Id INT PRIMARY KEY IDENTITY,
    RequesterId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    AddresseeId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Status INT NOT NULL, -- 0: Pending, 1: Accepted, 2: Declined
    RequestedDate DATETIME DEFAULT GETDATE(),
    RespondedDate DATETIME NULL,
    CONSTRAINT UQ_Friendship UNIQUE(RequesterId, AddresseeId)
);
```

#### Messages
```sql
CREATE TABLE Messages (
    Id INT PRIMARY KEY IDENTITY,
    SenderId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    ReceiverId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Content NVARCHAR(MAX) NOT NULL,
    SentDate DATETIME DEFAULT GETDATE(),
    IsRead BIT DEFAULT 0
);
```

#### Ratings
```sql
CREATE TABLE Ratings (
    Id INT PRIMARY KEY IDENTITY,
    ProblemId INT NOT NULL FOREIGN KEY REFERENCES Problems(Id),
    StudentId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    TutorId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Stars INT NOT NULL CHECK(Stars >= 1 AND Stars <= 5),
    Comment NVARCHAR(MAX),
    CreatedDate DATETIME DEFAULT GETDATE()
);
```

### Community Features

#### CommunityPosts
```sql
CREATE TABLE CommunityPosts (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Title NVARCHAR(500),
    Content NVARCHAR(MAX) NOT NULL,
    Category NVARCHAR(100),
    CreatedAt DATETIME DEFAULT GETDATE(),
    IsDeleted BIT DEFAULT 0
);
```

#### CommunityComments
```sql
CREATE TABLE CommunityComments (
    Id INT PRIMARY KEY IDENTITY,
    PostId INT NOT NULL FOREIGN KEY REFERENCES CommunityPosts(Id),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Content NVARCHAR(MAX) NOT NULL,
    IsAnonymous BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    IsDeleted BIT DEFAULT 0
);
```

#### CommunityPostLikes / CommunityCommentLikes
```sql
CREATE TABLE CommunityPostLikes (
    Id INT PRIMARY KEY IDENTITY,
    PostId INT NOT NULL FOREIGN KEY REFERENCES CommunityPosts(Id),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    ReactionType NVARCHAR(20) NOT NULL, -- like, love, haha, angry
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_PostLike UNIQUE(PostId, UserId)
);

CREATE TABLE CommunityCommentLikes (
    Id INT PRIMARY KEY IDENTITY,
    CommentId INT NOT NULL FOREIGN KEY REFERENCES CommunityComments(Id),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    ReactionType NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_CommentLike UNIQUE(CommentId, UserId)
);
```

### Group Features

#### ProblemGroups
```sql
CREATE TABLE ProblemGroups (
    Id INT PRIMARY KEY IDENTITY,
    ProblemId INT NOT NULL FOREIGN KEY REFERENCES Problems(Id),
    CreatedByUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    GroupName NVARCHAR(200),
    TotalPrice DECIMAL(18,2) NOT NULL,
    PricePerMember DECIMAL(18,2) NOT NULL,
    MaxMembers INT DEFAULT 10,
    CreatedDate DATETIME DEFAULT GETDATE()
);
```

#### ProblemGroupMembers
```sql
CREATE TABLE ProblemGroupMembers (
    Id INT PRIMARY KEY IDENTITY,
    GroupId INT NOT NULL FOREIGN KEY REFERENCES ProblemGroups(Id),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    JoinedDate DATETIME DEFAULT GETDATE(),
    PaymentStatus INT DEFAULT 0, -- 0: Unpaid, 1: Paid
    CONSTRAINT UQ_GroupMember UNIQUE(GroupId, UserId)
);
```

#### ProblemGroupInvites
```sql
CREATE TABLE ProblemGroupInvites (
    Id INT PRIMARY KEY IDENTITY,
    GroupId INT NOT NULL FOREIGN KEY REFERENCES ProblemGroups(Id),
    InvitedUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    InvitedByUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Status INT DEFAULT 0, -- 0: Pending, 1: Accepted, 2: Rejected
    InvitedDate DATETIME DEFAULT GETDATE(),
    RespondedDate DATETIME NULL
);
```

#### GroupPayments
```sql
CREATE TABLE GroupPayments (
    Id INT PRIMARY KEY IDENTITY,
    GroupId INT NOT NULL FOREIGN KEY REFERENCES ProblemGroups(Id),
    MemberId INT NOT NULL FOREIGN KEY REFERENCES ProblemGroupMembers(Id),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Amount DECIMAL(18,2) NOT NULL,
    Status INT DEFAULT 0, -- 0: Pending, 1: Completed, 2: Failed
    CreatedDate DATETIME DEFAULT GETDATE(),
    CompletedDate DATETIME NULL,
    TransactionId NVARCHAR(200)
);
```

### School Features

#### SchoolClasses
```sql
CREATE TABLE SchoolClasses (
    Id INT PRIMARY KEY IDENTITY,
    ClassName NVARCHAR(200) NOT NULL,
    Grade NVARCHAR(50),
    AcademicYear NVARCHAR(50),
    TeacherName NVARCHAR(200),
    StudentCount INT,
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME,
    IsActive BIT DEFAULT 1
);
```

## 📡 API Documentation

### Authentication Endpoints
```
POST /Account/Login
POST /Account/Register
POST /Account/Logout
```

### Student Endpoints
```
GET  /Student/Dashboard
GET  /Student/MyProblems
POST /Student/CreateProblem
GET  /Student/ProblemDetails/{id}
POST /Student/EditProblem/{id}
POST /Student/DeleteProblem/{id}
GET  /Student/RateTutor/{problemId}
POST /Student/RateTutor
GET  /Student/PaymentCheckout/{paymentId}
GET  /Student/GroupDetails/{id}
POST /Student/InviteFriend
POST /Student/AcceptGroupInvite/{inviteId}
POST /Student/LeaveGroup/{groupId}
```

### Tutor Endpoints
```
GET  /Tutor/Dashboard
GET  /Tutor/AvailableProblems
GET  /Tutor/MyProblems
POST /Tutor/AcceptProblem/{id}
GET  /Tutor/SubmitSolution?problemId={id}
POST /Tutor/SubmitSolution
GET  /Tutor/MyRatings
GET  /Tutor/Earnings
```

### Community API (SignalR + REST)
```
GET  /Student/GetAllPosts
GET  /Student/GetPostComments?postId={id}
POST /Student/CreatePost
POST /Student/PostComment
POST /Student/ReactToPost
POST /Student/ReactToComment
POST /Student/UpdatePost
POST /Student/DeletePost
```

### Admin Endpoints
```
GET  /Admin/Dashboard
GET  /Admin/ManageUsers
GET  /Admin/ManageTutors
GET  /Admin/PendingTutors
POST /Admin/ApproveTutor/{id}
GET  /Admin/ManageProblems
POST /Admin/DeleteProblem/{id}
GET  /Admin/Statistics
```

### Premium Endpoints
```
GET  /Student/Premium
POST /Student/UpgradePremium
GET  /Tutor/Premium
POST /Tutor/UpgradePremium
```

## 🎨 Screenshots

### 🏠 Landing Page
Trang chủ với giới thiệu hệ thống và lựa chọn vai trò

### 👨‍🎓 Student Dashboard
- Thống kê bài toán (Pending, In Progress, Solved)
- Danh sách bài toán gần đây
- Trạng thái thanh toán
- Biểu đồ thống kê

### 📝 Create Problem
- Form đăng bài với đầy đủ trường thông tin
- Upload hình ảnh và file đính kèm
- Chọn độ khó và tính giá tự động
- Tùy chọn tạo nhóm học tập

### 👥 Group Details
- Danh sách thành viên nhóm
- Trạng thái thanh toán của từng thành viên
- Mời bạn bè tham gia
- Quản lý lời mời

### 💬 Community
- Feed bài viết
- Real-time comments
- Reaction system (Like, Love, Haha, Angry)
- Anonymous posting

### 📱 Chat & Messages
- Real-time chat với SignalR
- Danh sách cuộc trò chuyện
- Unread message indicators
- Typing indicators

### 👨‍🏫 Mentor Dashboard
- Bài toán có sẵn
- Bài đang thực hiện
- Tổng thu nhập
- Đánh giá trung bình

### 🤖 AI Assistant
- Chat với Gemini AI
- Lịch sử chat
- Hỗ trợ giải bài

### 💳 Payment Checkout
- Tích hợp SePay Gateway
- Thanh toán an toàn
- Webhook xác nhận

### 👨‍💼 Admin Dashboard
- Tổng quan hệ thống
- Số liệu thống kê
- Biểu đồ doanh thu
- Quản lý người dùng

## 🚀 Deployment

### Build for Production
```bash
dotnet publish -c Release -o ./publish
```

### Docker (Optional)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Webgiasu.csproj", "."]
RUN dotnet restore "Webgiasu.csproj"
COPY . .
RUN dotnet build "Webgiasu.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Webgiasu.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Webgiasu.dll"]
```

Build Docker image:
```bash
docker build -t learnwithmentor:latest .
docker run -d -p 8080:80 --name learnwithmentor-app learnwithmentor:latest
```

## 🤝 Đóng góp

Mọi đóng góp đều được chào đón! Vui lòng:

1. Fork repository
2. Tạo branch mới (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Mở Pull Request

## 📝 License

Dự án này được cấp phép theo giấy phép MIT - xem file [LICENSE](LICENSE) để biết thêm chi tiết.

## 👥 Contributors

- **Toàn Huỳnh** - [GitHub](https://github.com/Toan-Huynh2054)
- **PMHieu** - Branch contributor

## 🙏 Acknowledgments

- ASP.NET Core Team
- Bootstrap Team
- SignalR Team
- Google Gemini AI
- SePay Payment Gateway
- Open Source Community

---

⭐ **Nếu bạn thấy dự án hữu ích, hãy cho một star!** ⭐
