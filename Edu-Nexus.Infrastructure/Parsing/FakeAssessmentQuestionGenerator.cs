using Edu_Nexus.Application.Interfaces.Parsing;

namespace Edu_Nexus.Infrastructure.Parsing;

/// Heuristic question generator. Picks from a small pre-written bank keyed by skill name,
/// falling back to generic templated questions when an unknown skill comes in.
/// Replace with LLM-backed generator by swapping the DI registration.
public class FakeAssessmentQuestionGenerator : IAssessmentQuestionGenerator
{
    private static readonly Dictionary<string, List<GeneratedQuestion>> Bank = BuildBank();

    public Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(
        AssessmentGenerationInput input,
        CancellationToken cancellationToken = default)
    {
        var skills = input.HardSkills.Count == 0
            ? new[] { "Programming Fundamentals" }
            : input.HardSkills.ToArray();

        var part1 = BuildSubset(skills, 1, input.Part1Target);
        var part2 = BuildSubset(skills, 2, input.Part2Target);

        var combined = part1.Concat(part2).ToList();
        return Task.FromResult<IReadOnlyList<GeneratedQuestion>>(combined);
    }

    private static List<GeneratedQuestion> BuildSubset(string[] skills, int part, int target)
    {
        var result = new List<GeneratedQuestion>();
        var skillIndex = 0;
        var pickIndex = 0;

        while (result.Count < target)
        {
            var skill = skills[skillIndex % skills.Length];
            var pool = Bank.TryGetValue(skill, out var found) ? found : GenericPool(skill);
            var question = pool[pickIndex % pool.Count];

            result.Add(question with { Part = part });

            skillIndex++;
            if (skillIndex % skills.Length == 0) pickIndex++;
        }

        return result;
    }

    private static List<GeneratedQuestion> GenericPool(string skill) => new()
    {
        new GeneratedQuestion(1, skill,
            $"Đâu là đặc điểm chính của {skill}?",
            $"{skill} chỉ hoạt động trên Windows",
            $"{skill} là công nghệ được sử dụng rộng rãi trong phát triển phần mềm",
            $"{skill} không hỗ trợ đa luồng",
            $"{skill} đã ngừng phát triển",
            "B",
            $"{skill} là một công nghệ phổ biến và được duy trì tích cực."),
        new GeneratedQuestion(1, skill,
            $"Khi mới học {skill}, bạn nên bắt đầu từ đâu?",
            "Học cú pháp cơ bản và làm các bài tập nhỏ",
            "Đọc toàn bộ source code của framework",
            "Bỏ qua tài liệu chính thức",
            "Học framework nâng cao trước",
            "A",
            "Nắm cú pháp + thực hành bài tập nhỏ là lộ trình hiệu quả nhất khi bắt đầu."),
    };

    private static Dictionary<string, List<GeneratedQuestion>> BuildBank()
    {
        var bank = new Dictionary<string, List<GeneratedQuestion>>(StringComparer.OrdinalIgnoreCase);

        bank["Java"] = new()
        {
            new GeneratedQuestion(1, "Java",
                "Trong Java, interface khác abstract class ở điểm nào sau đây?",
                "Interface có thể có biến instance",
                "Abstract class có thể có constructor, interface thì không",
                "Interface có thể kế thừa nhiều abstract class",
                "Cả hai đều giống hệt nhau",
                "B",
                "Abstract class có constructor để khởi tạo state; interface (trước Java 8) không có state nên không có constructor."),
            new GeneratedQuestion(1, "Java",
                "Method overriding khác overloading ở điểm nào?",
                "Overriding xảy ra trong cùng 1 class",
                "Overloading thay đổi cả tên method",
                "Overriding xảy ra giữa class cha và class con với cùng signature",
                "Cả hai cần annotation @Override",
                "C",
                "Overriding: cùng signature, ở subclass. Overloading: cùng tên, khác parameter list trong cùng class."),
        };

        bank["Spring Boot"] = new()
        {
            new GeneratedQuestion(1, "Spring Boot",
                "Annotation nào dùng để đánh dấu một class là REST controller trong Spring Boot?",
                "@Controller",
                "@RestController",
                "@Service",
                "@Component",
                "B",
                "@RestController = @Controller + @ResponseBody, response trả JSON mặc định."),
            new GeneratedQuestion(2, "Spring Boot",
                "Bạn cần expose endpoint GET /users/{id}. Cách viết nào đúng?",
                "@GetMapping(\"/users/{id}\") public User get(@RequestParam Long id)",
                "@GetMapping(\"/users/{id}\") public User get(@PathVariable Long id)",
                "@PostMapping(\"/users/{id}\") public User get(@PathVariable Long id)",
                "@RequestMapping(\"/users\") public User get(Long id)",
                "B",
                "Path parameter dùng @PathVariable, không phải @RequestParam (cái đó cho query string)."),
        };

        bank["Spring"] = bank["Spring Boot"];

        bank["C#"] = new()
        {
            new GeneratedQuestion(1, "C#",
                "Trong C#, từ khoá nào tạo ra một immutable reference type field?",
                "const",
                "readonly",
                "static",
                "sealed",
                "B",
                "readonly cho phép gán trong constructor; const yêu cầu giá trị compile-time."),
        };
        bank[".NET"] = bank["C#"];

        bank["PostgreSQL"] = new()
        {
            new GeneratedQuestion(1, "PostgreSQL",
                "Lệnh SQL nào để thêm cột mới vào bảng đã tồn tại?",
                "INSERT COLUMN ...",
                "ALTER TABLE table_name ADD COLUMN col_name TYPE",
                "UPDATE TABLE ADD ...",
                "CREATE COLUMN ...",
                "B",
                "ALTER TABLE là DDL chuẩn để thay đổi cấu trúc bảng."),
            new GeneratedQuestion(2, "PostgreSQL",
                "Bạn cần lấy 10 user mới nhất theo created_at. Query nào đúng?",
                "SELECT * FROM users LIMIT 10",
                "SELECT * FROM users ORDER BY created_at DESC LIMIT 10",
                "SELECT TOP 10 * FROM users",
                "SELECT * FROM users WHERE rownum <= 10",
                "B",
                "ORDER BY trước rồi LIMIT. TOP là cú pháp của SQL Server, rownum là Oracle."),
        };
        bank["MySQL"] = bank["PostgreSQL"];
        bank["SQL"] = bank["PostgreSQL"];

        bank["Docker"] = new()
        {
            new GeneratedQuestion(1, "Docker",
                "Lệnh nào tạo image từ Dockerfile trong thư mục hiện tại?",
                "docker run .",
                "docker build .",
                "docker create .",
                "docker compose up",
                "B",
                "docker build đọc Dockerfile và tạo image. docker run khởi chạy container từ image có sẵn."),
            new GeneratedQuestion(2, "Docker",
                "Bạn muốn map port 8080 của host vào port 80 trong container, chạy lệnh nào?",
                "docker run -p 80:8080 myapp",
                "docker run -p 8080:80 myapp",
                "docker run -port 8080 myapp",
                "docker run --expose 8080 myapp",
                "B",
                "Cú pháp -p HOST:CONTAINER. Vế trái là host, vế phải là container."),
        };

        bank["React"] = new()
        {
            new GeneratedQuestion(1, "React",
                "Hook nào dùng để giữ state trong functional component?",
                "useEffect",
                "useState",
                "useMemo",
                "useRef",
                "B",
                "useState trả về [value, setValue], là hook cơ bản nhất để quản lý state."),
            new GeneratedQuestion(2, "React",
                "Bạn cần gọi API khi component mount, nên dùng hook nào với dependency thế nào?",
                "useEffect(() => fetch(...), [])",
                "useState(() => fetch(...))",
                "useEffect(() => fetch(...)) (không deps)",
                "useMemo(() => fetch(...), [])",
                "A",
                "useEffect với deps array rỗng [] chạy 1 lần khi mount, giống componentDidMount."),
        };

        bank["JavaScript"] = new()
        {
            new GeneratedQuestion(1, "JavaScript",
                "Sự khác biệt giữa == và === là gì?",
                "Không có khác biệt",
                "== so sánh có ép kiểu, === không ép kiểu",
                "=== so sánh có ép kiểu, == không ép kiểu",
                "Cả hai đều ép kiểu",
                "B",
                "=== là strict equality (kiểu + giá trị), == là loose equality (ép kiểu trước khi so sánh)."),
        };
        bank["TypeScript"] = bank["JavaScript"];

        bank["Teamwork"] = new()
        {
            new GeneratedQuestion(2, "Teamwork",
                "Bạn không đồng tình với approach của teammate trong code review. Bạn nên làm gì?",
                "Reject PR không cần comment",
                "Approve cho qua để tránh xung đột",
                "Comment cụ thể lý do và đề xuất alternative",
                "Push thẳng commit của mình vào branch của họ",
                "C",
                "Code review hiệu quả cần feedback constructive với lý do rõ ràng và đề xuất khả thi."),
        };

        bank["English"] = new()
        {
            new GeneratedQuestion(2, "English",
                "Choose the correct sentence:",
                "I have working here since 2020.",
                "I have been working here since 2020.",
                "I work here since 2020.",
                "I worked here since 2020.",
                "B",
                "Present perfect continuous dùng cho hành động bắt đầu trong quá khứ và tiếp tục đến hiện tại."),
        };

        bank["Programming Fundamentals"] = new()
        {
            new GeneratedQuestion(1, "Programming Fundamentals",
                "Big-O notation O(n) mô tả gì?",
                "Bộ nhớ tối đa",
                "Thời gian chạy tăng tuyến tính theo kích thước input",
                "Thời gian chạy là hằng số",
                "Thuật toán không hợp lệ",
                "B",
                "O(n) = linear time complexity, thời gian tỉ lệ thuận với n."),
            new GeneratedQuestion(2, "Programming Fundamentals",
                "Khi nào nên dùng HashMap thay vì Array?",
                "Khi cần duyệt theo thứ tự",
                "Khi cần lookup theo key với O(1) trung bình",
                "Khi data có kích thước nhỏ",
                "Khi key là số nguyên liên tục",
                "B",
                "HashMap có lookup O(1) trung bình theo key, vượt trội Array O(n) khi cần truy xuất theo key."),
        };

        return bank;
    }
}
