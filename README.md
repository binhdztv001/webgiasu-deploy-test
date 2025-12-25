# H? th?ng Gia s? Online - ASP.NET Core MVC

## Gi?i thi?u
H? th?ng k?t n?i h?c sinh và gia s?, cho phép h?c sinh ??ng bài toán và nh?n l?i gi?i t? gia s?. H? th?ng s? d?ng **mock data** (không k?t n?i database).

## Công ngh? s? d?ng
- **ASP.NET Core 8.0 MVC**
- **Bootstrap 5** cho giao di?n
- **Bootstrap Icons** cho icon
- **Session** ?? qu?n lý ??ng nh?p
- **Mock Services** thay th? Database

## C?u trúc d? án

```
Webgiasu/
??? Controllers/           # Các controller x? lý logic
?   ??? HomeController.cs
?   ??? AccountController.cs
?   ??? StudentController.cs
?   ??? TutorController.cs
?   ??? AdminController.cs
??? Models/               # Các model d? li?u
?   ??? User.cs
?   ??? Problem.cs
?   ??? Solution.cs
?   ??? Payment.cs
?   ??? ViewModels/
??? Services/            # Mock services
?   ??? MockUserService.cs
?   ??? MockProblemService.cs
?   ??? MockSolutionService.cs
?   ??? MockPaymentService.cs
??? Views/              # Giao di?n
    ??? Home/
    ??? Account/
    ??? Student/
    ??? Tutor/
    ??? Admin/
```

## Các ch?c n?ng chính

### 1. H?c sinh (Student)
- ? ??ng ký / ??ng nh?p
- ? ??ng bài toán (tiêu ??, mô t?, môn h?c, ?? khó, deadline, hình ?nh)
- ? Xem danh sách bài toán ?ã ??ng
- ? Xem l?i gi?i t? gia s?
- ? ?ánh giá l?i gi?i (rating & feedback)
- ? Thanh toán (mock)
- ? Dashboard v?i th?ng kê

### 2. Gia s? (Tutor)
- ? ??ng ký / ??ng nh?p (c?n admin duy?t)
- ? Xem danh sách bài toán ch? gi?i
- ? Nh?n bài toán
- ? G?i l?i gi?i (text + file)
- ? Xem bài toán ?ã nh?n
- ? Xem l?i gi?i ?ã g?i
- ? Dashboard v?i th?ng kê thu nh?p

### 3. Admin
- ? Dashboard t?ng quan
- ? Duy?t tài kho?n gia s?
- ? Qu?n lý danh sách bài toán
- ? Qu?n lý thanh toán
- ? Th?ng kê chi ti?t (bài toán, doanh thu, top gia s?)
- ? Qu?n lý ng??i dùng
- ? Xóa bài toán

## Tài kho?n demo

### H?c sinh
- **Username**: `student1` / **Password**: `123456`
- **Username**: `student2` / **Password**: `123456`

### Gia s? (?ã ???c duy?t)
- **Username**: `tutor1` / **Password**: `123456`
- **Username**: `tutor2` / **Password**: `123456`

### Gia s? (ch?a ???c duy?t)
- **Username**: `tutor3` / **Password**: `123456`

### Admin
- **Username**: `admin` / **Password**: `admin123`

## Cách ch?y d? án

### Yêu c?u
- .NET 8.0 SDK
- Visual Studio 2022 ho?c VS Code
- Browser hi?n ??i (Chrome, Edge, Firefox)

### Các b??c

1. **Clone ho?c m? project**
```bash
cd Webgiasu
```

2. **Restore packages**
```bash
dotnet restore
```

3. **Build project**
```bash
dotnet build
```

4. **Run project**
```bash
dotnet run
```

5. **M? browser**
```
https://localhost:7xxx
ho?c
http://localhost:5xxx
```

## Tính n?ng n?i b?t

### ?? Giao di?n
- Bootstrap 5 responsive
- Bootstrap Icons
- Hover effects
- Alert messages
- Badge & progress bars

### ?? D? li?u Mock
- Không c?n database
- D? li?u l?u trong memory (List<T>)
- Reset khi restart app
- D? dàng test và demo

### ?? Xác th?c
- Session-based authentication
- Role-based authorization
- Redirect theo role
- Protected routes

### ?? Th?ng kê
- Dashboard cho t?ng role
- Bi?u ?? progress bar
- T?ng quan s? li?u
- Top performers

## M? r?ng trong t??ng lai

- [ ] K?t n?i Database (SQL Server / PostgreSQL)
- [ ] Entity Framework Core
- [ ] Upload file th?t
- [ ] Email notification
- [ ] Real-time chat
- [ ] Payment gateway integration
- [ ] Export PDF/Excel
- [ ] Search & Filter
- [ ] Pagination
- [ ] API cho mobile app

## L?u ý

?? **D? li?u mock**: M?i thay ??i s? m?t khi restart ?ng d?ng

?? **Upload file**: Ch? là mock, file không th?c s? ???c l?u

?? **Thanh toán**: Ch? c?p nh?t tr?ng thái, không có giao d?ch th?t

## C?u trúc Database (n?u mu?n m? r?ng)

```sql
Users (Id, Username, Password, FullName, Email, Phone, Role, IsApproved, RegisteredDate)
Problems (Id, StudentId, Title, Description, Type, Difficulty, ImageUrl, CreatedDate, Deadline, Status, AssignedTutorId, Price)
Solutions (Id, ProblemId, TutorId, Content, FileUrl, SubmittedDate, Rating, Feedback)
Payments (Id, StudentId, ProblemId, Amount, Status, CreatedDate, CompletedDate, TransactionId)
```

## Screenshots

### Trang ch?
- Ch?n vai trò (Student / Tutor / Admin)
- H??ng d?n s? d?ng

### Student Dashboard
- Th?ng kê bài toán
- Danh sách bài ?ã ??ng
- Tr?ng thái thanh toán

### Tutor Dashboard
- Bài toán ch? gi?i
- Bài ?ang th?c hi?n
- Thu nh?p

### Admin Dashboard
- T?ng quan h? th?ng
- Duy?t gia s?
- Th?ng kê chi ti?t

## Tác gi?
D? án ???c t?o ra cho m?c ?ích h?c t?p và demo.

## License
MIT License - T? do s? d?ng cho m?c ?ích h?c t?p.
