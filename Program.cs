using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace УмныйСад
   //добавил состояние растений
{
   public enum СостояниеРастения
    {
        Здоровое,
        Жаждущее,
        Вялое,
        Мёртвое
    }
    public interface IПоливаемый
    {
        void Полить(decimal литры);
    }
    public record ПоказанияСенсора(decimal Температура, decimal Влажность, decimal Освещённость, DateTime Время)
    {
        public override string ToString()
        {
            return $"Темп: {Температура:F1}°C, Влажность: {Влажность:F1}%, Свет: {Освещённость:F0} люкс ({Время:HH:mm:ss})";
        }
    }
    public class ИсключениеГибелиРастения : Exception
    {
        private string _имя;
        public ИсключениеГибелиРастения(string имя)
        {
            _имя = имя;
        }
        public override string Message => $"Растение '{_имя}' погибло :( (нужно пересадить)";
    }
    public class ИсключениеСенсора : Exception
    {
        private string _ид;
        public ИсключениеСенсора(string ид)
        {
            _ид = ид;
        }
        public override string Message => $"Сенсор '{_ид}' не отвечает!";
    }
    public abstract class Растение : IПоливаемый
    {
        protected string имя;
        protected decimal высота;
        protected СостояниеРастения состояние;
        protected Random случай = new Random();

        public string Имя => имя;
        public decimal Высота => высота;
        public СостояниеРастения Состояние => состояние;
        public Растение(string имя, decimal высота)
        {
            this.имя = имя;
            this.высота = высота;
            состояние = СостояниеРастения.Здоровое;
        }
        public abstract void Расти(ПоказанияСенсора данные);
        public virtual void Полить(decimal литры)
        {
            if (состояние == СостояниеРастения.Мёртвое) return;
            высота += литры * 0.3m;
            if (состояние == СостояниеРастения.Жаждущее)
                состояние = СостояниеРастения.Здоровое;
        }
        protected void Ухудшить()
        {
            if (состояние == СостояниеРастения.Здоровое)
                состояние = СостояниеРастения.Жаждущее;
            else if (состояние == СостояниеРастения.Жаждущее)
                состояние = СостояниеРастения.Вялое;
            else if (состояние == СостояниеРастения.Вялое)
                состояние = СостояниеРастения.Мёртвое;
        }
        public override string ToString()
        {
            return $"{имя} (высота: {высота:F1} см, состояние: {состояние})";
        }
        public override bool Equals(object? obj)
        {
            if (obj is not Растение другое) return false;
            return имя == другое.имя && состояние == другое.состояние;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(имя, состояние);
        }
    }
    public class ОвощноеРастение : Растение
    {
        private int днейБезПолива = 0;

        public ОвощноеРастение(string имя, decimal высота) : base(имя, высота) { }

        public override void Расти(ПоказанияСенсора данные)
        {
            if (данные.Влажность < 40)
            {
                днейБезПолива++;
                if (днейБезПолива > 2) Ухудшить();
            }
            else
            {
                днейБезПолива = 0;
                var рост = (данные.Освещённость / 10000m) * 0.5m;
                высота += рост;
            }

            if (данные.Температура > 45 || данные.Температура < 0)
                Ухудшить();
        }
    }
    public class ЦветущееРастение : Растение
    {
        private bool вЦвету = false;

        public ЦветущееРастение(string имя, decimal высота) : base(имя, высота) { }

        public override void Расти(ПоказанияСенсора данные)
        {
            if (данные.Освещённость > 20000 && данные.Влажность > 40 && данные.Температура > 10 && данные.Температура < 30)
            {
                высота += 0.4m;
                вЦвету = true;
            }
            else
            {
                вЦвету = false;
                Ухудшить();
            }
        }

        public override string ToString()
        {
            return base.ToString() + (вЦвету ? " 🌸" : "");
        }
    }
    public class Сенсор
    {
        public string Ид { get; }
        public string Тип { get; }
        Random rnd = new Random();

        public Сенсор(string ид, string тип)
        {
            Ид = ид;
            Тип = тип;
        }

        public ПоказанияСенсора Считать()
        {
            if (rnd.NextDouble() < 0.1)
                throw new ИсключениеСенсора(Ид);

            decimal t = 15 + (decimal)rnd.NextDouble() * 15;
            decimal h = 30 + (decimal)rnd.NextDouble() * 50;
            decimal l = 10000 + (decimal)rnd.NextDouble() * 50000;
            return new ПоказанияСенсора(t, h, l, DateTime.Now);
        }
    }
    public class Контроллер
    {
        List<Сенсор> сенсоры;
        Растение[] растения;
        Random rnd = new Random();

        public Контроллер(List<Сенсор> сенсоры, Растение[] растения)
        {
            this.сенсоры = сенсоры;
            this.растения = растения;
        }

        public void ЗапуститьЦикл()
        {
            Console.WriteLine("=== Начало цикла ===");

            var показания = new List<ПоказанияСенсора>();

            foreach (var s in сенсоры)
            {
                try
                {
                    var p = s.Считать();
                    Console.WriteLine($"Сенсор {s.Ид}: {p}");
                    показания.Add(p);
                }
                catch (ИсключениеСенсора ex)
                {
                    Console.WriteLine($"Ошибка сенсора: {ex.Message}");
                }
            }

            if (показания.Count == 0)
            {
                Console.WriteLine("Нет данных! Поливаем по чуть-чуть.");
                foreach (var р in растения)
                {
                    р.Полить(0.2m);
                    Console.WriteLine($"Полили {р}");
                }
                return;
            }

            var срТемп = показания.Average(x => x.Температура);
            var срВлаж = показания.Average(x => x.Влажность);
            var срСвет = показания.Average(x => x.Освещённость);

            var ср = new ПоказанияСенсора(срТемп, срВлаж, срСвет, DateTime.Now);
            Console.WriteLine($"Средние показания: {ср}");

            decimal литры = 0;
            if (срВлаж < 35) литры = 0.8m;
            else if (срВлаж < 45) литры = 0.5m;
            else if (срВлаж < 55) литры = 0.3m;
            else литры = 0;

            bool дождь = rnd.NextDouble() < 0.1;
            if (дождь)
            {
                Console.WriteLine("Пошёл дождь, уменьшаем полив!");
                литры *= 0.4m;
            }
            foreach (var р in растения)
            {
                try
                {
                    р.Расти(ср);
                    if (литры > 0)
                    {
                        р.Полить(литры);
                        Console.WriteLine($"Полили {р.Имя} на {литры} л. Текущее состояние: {р}");
                    }
                    else
                    {
                        Console.WriteLine($"Полив {р.Имя} не требуется. {р}");
                    }

                    if (р.Состояние == СостояниеРастения.Мёртвое)
                        throw new ИсключениеГибелиРастения(р.Имя);
                }
                catch (ИсключениеГибелиРастения ex)
                {
                    Console.WriteLine($"Критическая ошибка: {ex.Message}");
                }
            }

            ArrayList снимок = new ArrayList();
            foreach (var р in растения) снимок.Add(р.ToString());

            Console.WriteLine("Снимок теплицы: " + string.Join(" | ", снимок.ToArray()));

            Console.WriteLine("=== Конец цикла ===\n");
        }
    }
    internal class Программа
    {
        public static void Main()
        {
            Console.WriteLine("Добро пожаловать в систему 'Умный сад'!\n");

            var сенсоры = new List<Сенсор>()
            {
                new Сенсор("S1", "комбинированный"),
                new Сенсор("S2", "температурный"),
                new Сенсор("S3", "влажность")
            };

            Растение[] растения =
            {
                new ОвощноеРастение("Помидор", 12.5m),
                new ОвощноеРастение("Огурец", 10.1m),
                new ЦветущееРастение("Роза", 15.3m)
            };

            var контроллер = new Контроллер(сенсоры, растения);

            for (int день = 1; день <= 5; день++)
            {
                Console.WriteLine($"=== День {день} ===");
                контроллер.ЗапуститьЦикл();
                Thread.Sleep(500);
            }

            Console.WriteLine("Работа теплицы завершена. До свидания!");
        }
    }
}
