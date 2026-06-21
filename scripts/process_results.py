import os
import glob
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt

# Устанавливаем красивый стиль графиков
plt.style.use('seaborn-v0_8-whitegrid' if 'seaborn-v0_8-whitegrid' in plt.style.available else 'default')
plt.rcParams.update({
    'font.size': 11,
    'axes.labelsize': 12,
    'axes.titlesize': 14,
    'xtick.labelsize': 10,
    'ytick.labelsize': 10,
    'figure.titlesize': 16
})

results_dir = "results/D2"
plots_dir = "results/plots"
os.makedirs(plots_dir, exist_ok=True)

def plot_convergence(func_name):
    # Заменяем пробелы на подчеркивания
    sanitized_name = func_name.replace(" ", "_")
    func_path = os.path.join(results_dir, sanitized_name)
    if not os.path.exists(func_path):
        # Попробуем альтернативные варианты имени
        alternatives = glob.glob(os.path.join(results_dir, func_name.replace(" ", "*").replace(".", "*")))
        if alternatives:
            func_path = alternatives[0]
        else:
            print(f"Directory not found for {func_name}")
            return False

    plt.figure(figsize=(9, 5))
    
    optimizers = {
        "AntAnnealingOptimizer": ("ACO-SA (Гибрид)", "#2ca02c", 200),
        "SimulatedAnnealingOptimizer": ("SA (Имитация отжига)", "#ff7f0e", 1000),
        "GradientDescentOptimizer": ("GD (Градиентный спуск)", "#1f77b4", 1000)
    }
    
    has_data = False
    for opt_file_prefix, (label, color, max_show_iter) in optimizers.items():
        # Ищем все 30 файлов трассировки
        files = glob.glob(os.path.join(func_path, f"{opt_file_prefix}_run*_trace.csv"))
        if not files:
            continue
            
        all_traces = []
        for f in files:
            try:
                df = pd.read_csv(f)
                all_traces.append(df['bestFx'].values)
            except Exception as e:
                print(f"Error reading {f}: {e}")
                
        if all_traces:
            # Находим минимальную длину среди всех трасс
            min_len = min(len(t) for t in all_traces)
            # Обрезаем до min_len и собираем в массив
            traces_matrix = np.array([t[:min_len] for t in all_traces])
            
            # Считаем среднее по каждому шагу
            mean_trace = np.mean(traces_matrix, axis=0)
            
            # Ограничиваем количество итераций на графике для наглядности
            show_len = min(min_len, max_show_iter)
            x = np.arange(show_len)
            y = mean_trace[:show_len]
            
            # Корректируем отрицательные или нулевые значения для логарифмической шкалы
            y_log = np.maximum(y, 1e-16)
            
            plt.semilogy(x, y_log, label=label, color=color, linewidth=2)
            has_data = True

    if has_data:
        plt.title(f"График сходимости на функции {func_name} (D = 2, усреднение по 30 запускам)")
        plt.xlabel("Итерация")
        plt.ylabel("Значение функции f(x) (логарифмическая шкала)")
        plt.legend(frameon=True)
        plt.ylim(bottom=1e-17)
        plt.tight_layout()
        
        # Добавляем _v2 в имя файла для сброса кэша на GitHub
        plot_file = os.path.join(plots_dir, f"{func_name.lower().replace(' ', '_').replace('.', '_')}_convergence_v2.png")
        plt.savefig(plot_file, dpi=150)
        plt.close()
        print(f"Saved plot: {plot_file}")
        return True
    else:
        plt.close()
        return False

# Строим графики для двух показательных функций
plot_convergence("Sphere")
plot_convergence("Rastrigin")

# Генерация таблицы результатов для README.md
summary_file = os.path.join(results_dir, "summary_report.csv")
if os.path.exists(summary_file):
    df_summary = pd.read_csv(summary_file)
    
    pivoted = df_summary.pivot(index="Function", columns="Optimizer")
    
    # Создаем Markdown таблицу
    md_lines = []
    md_lines.append("| Тестовая функция | ACO-SA (Успех) | ACO-SA (Среднее f) | SA (Успех) | SA (Среднее f) | GD (Успех) | GD (Среднее f) |")
    md_lines.append("| :--- | :---: | :---: | :---: | :---: | :---: | :---: |")
    
    # Сортируем функции по имени или порядку
    functions_order = [
        "Sphere", "Elliptic", "SumSquare", "sumPower", "Schwefel 2.22", "Schwefel 2.21", 
        "Step", "Exponential", "Quartic", "Rosenbrock", "Rastrigin", "NCRastrigin", 
        "Griewank", "Schwefel2.26", "Ackley", "Penalized1", "Penalized2", "Alpine", 
        "Levy", "Weierstrass", "Styblinski-Tang", "Michalewicz"
    ]
    
    existing_funcs = pivoted.index.tolist()
    sorted_funcs = [f for f in functions_order if f in existing_funcs]
    sorted_funcs += [f for f in existing_funcs if f not in sorted_funcs]
    
    for func in sorted_funcs:
        row = pivoted.loc[func]
        
        aco_success = f"{row[('SuccessRate', 'AntAnnealingOptimizer')]*100:.0f}%"
        aco_mean = f"{row[('MeanFx', 'AntAnnealingOptimizer')]:.3E}"
        
        sa_success = f"{row[('SuccessRate', 'SimulatedAnnealingOptimizer')]*100:.0f}%"
        sa_mean = f"{row[('MeanFx', 'SimulatedAnnealingOptimizer')]:.3E}"
        
        gd_success = f"{row[('SuccessRate', 'GradientDescentOptimizer')]*100:.0f}%"
        gd_mean = f"{row[('MeanFx', 'GradientDescentOptimizer')]:.3E}"
        
        md_lines.append(f"| **{func}** | {aco_success} | `{aco_mean}` | {sa_success} | `{sa_mean}` | {gd_success} | `{gd_mean}` |")
        
    md_table = "\n".join(md_lines)
    
    with open("results/readme_table.md", "w", encoding="utf-8") as f:
        f.write(md_table)
    print("Generated markdown table: results/readme_table.md")
else:
    print("summary_report.csv not found")
