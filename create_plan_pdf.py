from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate, Frame, PageTemplate, Paragraph, Spacer,
    Table, TableStyle, PageBreak, KeepTogether
)

OUT = "/home/rosik/FinanceTracker/План_разработки_FinanceTracker.pdf"
REGULAR = "/usr/share/fonts/liberation-sans-fonts/LiberationSans-Regular.ttf"
BOLD = "/usr/share/fonts/liberation-sans-fonts/LiberationSans-Bold.ttf"

pdfmetrics.registerFont(TTFont("Liberation", REGULAR))
pdfmetrics.registerFont(TTFont("Liberation-Bold", BOLD))

PAGE_W, PAGE_H = A4
BLUE = colors.HexColor("#2379CF")
DARK = colors.HexColor("#152331")
MUTED = colors.HexColor("#647789")
LIGHT = colors.HexColor("#EEF4F8")
GREEN = colors.HexColor("#15835C")
RED = colors.HexColor("#C7474B")

styles = getSampleStyleSheet()
title = ParagraphStyle("TitleRu", fontName="Liberation-Bold", fontSize=25, leading=30,
                       textColor=DARK, alignment=TA_CENTER, spaceAfter=10)
subtitle = ParagraphStyle("SubtitleRu", fontName="Liberation", fontSize=11, leading=16,
                          textColor=MUTED, alignment=TA_CENTER, spaceAfter=18)
h1 = ParagraphStyle("H1Ru", fontName="Liberation-Bold", fontSize=16, leading=21,
                    textColor=DARK, spaceBefore=9, spaceAfter=8)
h2 = ParagraphStyle("H2Ru", fontName="Liberation-Bold", fontSize=12, leading=16,
                    textColor=BLUE, spaceBefore=6, spaceAfter=5)
body = ParagraphStyle("BodyRu", fontName="Liberation", fontSize=9.5, leading=14,
                      textColor=colors.HexColor("#263746"), spaceAfter=5)
bullet = ParagraphStyle("BulletRu", parent=body, leftIndent=14, firstLineIndent=-8, bulletIndent=5, spaceAfter=3)
small = ParagraphStyle("SmallRu", fontName="Liberation", fontSize=8, leading=11, textColor=MUTED)
code = ParagraphStyle("CodeRu", fontName="Courier", fontSize=8.2, leading=11,
                      leftIndent=8, rightIndent=8, borderColor=colors.HexColor("#C8D6E1"),
                      borderWidth=0.5, borderPadding=7, backColor=colors.HexColor("#F6F9FB"), spaceAfter=8)
callout = ParagraphStyle("CalloutRu", fontName="Liberation-Bold", fontSize=10.5, leading=15,
                         textColor=colors.white, backColor=BLUE, borderPadding=10,
                         spaceBefore=5, spaceAfter=12)


def p(text, style=body):
    return Paragraph(text, style)


def bullets(items):
    return [Paragraph("• " + item, bullet) for item in items]


def section(number, heading, paragraphs=None, items=None):
    result = [Paragraph(f"{number}. {heading}", h1)]
    for text in paragraphs or []:
        result.append(p(text))
    result.extend(bullets(items or []))
    result.append(Spacer(1, 3 * mm))
    return result


def header_footer(canvas, doc):
    canvas.saveState()
    canvas.setStrokeColor(colors.HexColor("#D7E2EA"))
    canvas.setLineWidth(0.5)
    canvas.line(18 * mm, PAGE_H - 15 * mm, PAGE_W - 18 * mm, PAGE_H - 15 * mm)
    canvas.setFont("Liberation", 8)
    canvas.setFillColor(MUTED)
    canvas.drawString(18 * mm, PAGE_H - 11 * mm, "FinanceTracker — план разработки")
    canvas.drawRightString(PAGE_W - 18 * mm, 11 * mm, f"Страница {doc.page}")
    canvas.restoreState()


doc = BaseDocTemplate(
    OUT, pagesize=A4, leftMargin=18 * mm, rightMargin=18 * mm,
    topMargin=21 * mm, bottomMargin=18 * mm,
    title="План разработки FinanceTracker",
    author="FinanceTracker Project"
)
frame = Frame(doc.leftMargin, doc.bottomMargin, doc.width, doc.height, id="normal")
doc.addPageTemplates(PageTemplate(id="main", frames=[frame], onPage=header_footer))

story = []
story += [Spacer(1, 22 * mm), p("FinanceTracker", title),
          p("Пошаговый план создания настольного приложения для учёта личных финансов", subtitle)]

stack_data = [
    [p("Компонент", h2), p("Выбор", h2), p("Назначение", h2)],
    [p("Язык"), p("C#"), p("Основная логика приложения")],
    [p("Платформа"), p(".NET 8 LTS"), p("Стабильная среда выполнения")],
    [p("Интерфейс"), p("Avalonia UI"), p("Нативный интерфейс Linux и Windows")],
    [p("База данных"), p("SQLite"), p("Локальное хранение без отдельного сервера")],
    [p("Архитектура"), p("MVVM"), p("Разделение интерфейса, логики и данных")],
    [p("Тесты"), p("xUnit"), p("Проверка расчётов и работы базы")],
]
stack_table = Table(stack_data, colWidths=[37 * mm, 42 * mm, 92 * mm], repeatRows=1)
stack_table.setStyle(TableStyle([
    ("BACKGROUND", (0, 0), (-1, 0), LIGHT),
    ("GRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#C8D6E1")),
    ("VALIGN", (0, 0), (-1, -1), "TOP"),
    ("LEFTPADDING", (0, 0), (-1, -1), 7),
    ("RIGHTPADDING", (0, 0), (-1, -1), 7),
    ("TOPPADDING", (0, 0), (-1, -1), 6),
    ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
]))
story += [stack_table, Spacer(1, 9 * mm), p(
    "Оптимальный стек: C# + .NET 8 + Avalonia UI + SQLite. Приложение работает без браузера, "
    "хранит данные на компьютере и устанавливается в меню GNOME как обычная программа.", callout),
    p("Windows Forms не выбран, потому что официально работает только на Windows. Avalonia сохраняет C# и XAML, но поддерживает Fedora Linux и Windows.", body),
    PageBreak()]

story += section(1, "Определить минимальную версию (MVP)", items=[
    "Добавление доходов и расходов.",
    "Редактирование и удаление операций с подтверждением.",
    "История операций и фильтр по месяцу.",
    "Расчёт доходов, расходов и текущего баланса.",
    "Локальное сохранение после закрытия программы.",
    "Запуск из меню приложений без браузера."
])
story += section(2, "Подготовить инструменты", items=[
    ".NET 8 SDK для сборки проекта.",
    "Avalonia Templates для создания интерфейса.",
    "Visual Studio Code или JetBrains Rider.",
    "Git для истории изменений и публикации на GitHub."
])
story += section(3, "Создать структуру решения", paragraphs=[
    "Проект разделяется на интерфейс, бизнес-логику, базу данных и тесты."
])
story += [p("""FinanceTracker/<br/>
├── src/<br/>
│   ├── FinanceTracker.App/       — окна и ViewModel<br/>
│   ├── FinanceTracker.Core/      — модели и расчёты<br/>
│   └── FinanceTracker.Data/      — SQLite и репозитории<br/>
├── tests/<br/>
│   └── FinanceTracker.Tests/     — автоматические тесты<br/>
├── FinanceTracker.sln<br/>
├── README.md<br/>
└── .gitignore""", code)]
story += section(4, "Создать модель операции", paragraphs=[
    "Поля модели: Id, Type, AmountMinor, Category, Description и Date. Type принимает только Income или Expense."
], items=[
    "Сумма хранится в целых копейках: 150,25 грн → 15025.",
    "Это исключает ошибки округления денежных значений.",
    "Дата сохраняется в формате ISO 8601: yyyy-MM-dd."
])
story += section(5, "Добавить правила проверки", items=[
    "Сумма должна быть больше нуля.",
    "Тип и категория обязательны.",
    "Описание — не более 500 символов.",
    "Редактировать или удалять можно только существующую запись."
])

story += section(6, "Создать SQLite-базу", paragraphs=[
    "Файл базы хранится в ~/.local/share/FinanceTracker/finance.db. Он не должен попадать в GitHub."
])
schema = [["Таблица", "Содержимое"],
          ["Transactions", "Доходы и расходы"],
          ["Categories", "Стандартные и пользовательские категории"],
          ["Budgets", "Лимит расходов по годам и месяцам"],
          ["Settings", "Валюта и пользовательские параметры"]]
t = Table([[p(str(c), h2 if r == 0 else body) for c in row] for r, row in enumerate(schema)], colWidths=[48 * mm, 120 * mm], repeatRows=1)
t.setStyle(TableStyle([("GRID", (0,0), (-1,-1), .5, colors.HexColor("#C8D6E1")), ("BACKGROUND", (0,0), (-1,0), LIGHT), ("VALIGN", (0,0), (-1,-1), "TOP"), ("PADDING", (0,0), (-1,-1), 7)]))
story += [t, Spacer(1, 5 * mm)]
story += section(7, "Реализовать репозиторий", items=[
    "GetAllAsync — получить операции.",
    "AddAsync — добавить операцию.",
    "UpdateAsync — изменить операцию.",
    "DeleteAsync — удалить операцию.",
    "GetCategoriesAsync и AddCategoryAsync — работа с категориями.",
    "GetMonthlyBudgetAsync и SetMonthlyBudgetAsync — работа с бюджетом.",
    "Использовать параметры SQL, не склеивать запросы строками."
])
story += section(8, "Собрать главное окно", items=[
    "Боковое меню: Главная, Операции, Статистика, Бюджет, Категории, Настройки.",
    "Карточки: доходы, расходы, баланс и бюджет.",
    "Последние операции и форма быстрого добавления.",
    "Тёмная тема с зелёным цветом доходов, красным — расходов и синим — баланса."
])
story += section(9, "Реализовать раздел «Операции»", items=[
    "Полная таблица операций.",
    "Поиск по категории и описанию.",
    "Фильтры по месяцу и типу.",
    "Редактирование выбранной записи.",
    "Удаление только после подтверждения.",
    "Сортировка от новых операций к старым."
])
story += section(10, "Реализовать статистику", items=[
    "Суммы доходов и расходов за выбранный месяц.",
    "Распределение расходов по категориям.",
    "Процент каждой категории.",
    "Сравнение фактических расходов с бюджетом.",
    "Для первой версии достаточно ProgressBar; диаграммы можно добавить позже."
])
story += section(11, "Реализовать бюджет", paragraphs=[
    "Пользователь выбирает месяц и устанавливает лимит. Приложение показывает потраченную сумму и процент использования. При превышении лимита индикатор становится красным."
])
story += section(12, "Реализовать категории", items=[
    "Расходы: продукты, транспорт, покупки, развлечения, здоровье, коммунальные услуги, рестораны, образование, другое.",
    "Доходы: зарплата, подработка, подарок, возврат, другое.",
    "Пользователь может добавлять собственные категории."
])
story += section(13, "Добавить настройки", items=[
    "Выбор обозначения валюты.",
    "Просмотр пути к базе данных.",
    "Открытие папки данных.",
    "Экспорт истории в CSV.",
    "Создание и восстановление резервной копии."
])
story += section(14, "Написать автоматические тесты", items=[
    "Расчёт доходов, расходов и баланса.",
    "Округление денежных значений.",
    "Проверка некорректной суммы и категории.",
    "Добавление, изменение и удаление записи SQLite.",
    "Сохранение бюджета и загрузка категорий."
])
story += section(15, "Провести ручное тестирование", items=[
    "Запуск с пустой базой.",
    "Все CRUD-операции.",
    "Сохранение после перезапуска.",
    "Фильтры и поиск.",
    "Отрицательный баланс и превышение бюджета.",
    "Запуск установленной версии из меню GNOME."
])
story += section(16, "Собрать самостоятельное приложение", paragraphs=[
    "Выполнить self-contained публикацию для Linux x64, чтобы пользователю не требовалась отдельная установка .NET."
])
story += [p("dotnet publish src/FinanceTracker.App -c Release -r linux-x64 --self-contained true", code)]
story += section(17, "Установить приложение на ПК", items=[
    "Программа: ~/.local/lib/FinanceTracker/",
    "Данные: ~/.local/share/FinanceTracker/finance.db",
    "Ярлык: ~/.local/share/applications/financetracker.desktop",
    "Иконка: ~/.local/share/icons/hicolor/scalable/apps/financetracker.svg"
])
story += section(18, "Оформить GitHub", items=[
    "README с описанием, скриншотами и инструкцией запуска.",
    "Описание архитектуры и используемых технологий.",
    "Список реализованных функций и план развития.",
    "Не публиковать bin, obj, finance.db и реальные финансовые данные."
])

story += [Spacer(1, 5 * mm), p("Порядок реализации", h1)]
roadmap = [
    ["Этап", "Результат"],
    ["1. Core", "Модель, деньги, расчёты и валидация"],
    ["2. Data", "SQLite, CRUD, категории и бюджет"],
    ["3. App", "Главное окно и навигация"],
    ["4. Features", "Операции, фильтры, статистика и экспорт"],
    ["5. Quality", "Автоматические и ручные тесты"],
    ["6. Delivery", "Self-contained сборка, ярлык и GitHub"]
]
rt = Table([[p(c, h2 if i == 0 else body) for c in row] for i, row in enumerate(roadmap)], colWidths=[42 * mm, 126 * mm], repeatRows=1)
rt.setStyle(TableStyle([("GRID", (0,0), (-1,-1), .5, colors.HexColor("#C8D6E1")), ("BACKGROUND", (0,0), (-1,0), LIGHT), ("VALIGN", (0,0), (-1,-1), "TOP"), ("PADDING", (0,0), (-1,-1), 7)]))
story += [rt, Spacer(1, 8 * mm), p(
    "Итог: C# + .NET 8 + Avalonia UI + SQLite — оптимальная основа для настоящего устанавливаемого FinanceTracker на Fedora Linux и Windows.", callout)]

doc.build(story)
print(OUT)
