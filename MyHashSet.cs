using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyHashSet
{
    public class MyHashSet<T>
    {
        /// <summary>
        /// Массив слотов с записями хеш-таблицы. Имеет тот же размер, что и _buckets.
        /// В нормальной ситуции массив слотов заполняется подряд.
        /// </summary>
        private Slot[] _slots;
        
        /// <summary>
        /// Таблица, в которой хранится индекс слота (увеличенный на 1), с которого нужно начинать поиск в хеш-таблице.
        /// Имеет тот же размер, что и _slots. Задает начало цепочки для заданного хеш-значения.
        /// </summary>
        private int[] _buckets;
        
        /// <summary>
        /// Общее количество заполненных эдементов в таблицах.
        /// </summary>
        private int _count;

        /// <summary>
        /// Индекс элемента, в который будет производиться вставка в случае, если отсутствуют свободные после удаления 
        /// ячейки (_freeList == -1).
        /// </summary>
        private int _lastIndex;

        /// <summary>
        /// Индекс начала списка свободных слотов (цепочка всегда заканчивается -1).
        /// Следующий элемент списка содержится в _slots[_freeList].Next и так далее по списку через Next до -1.
        /// </summary>
        private int _freeList;

        /// <summary>
        /// Слот хеш-таблицы.
        /// </summary>
        internal struct Slot
        {
            /// <summary>
            /// Хеш-код хранимого значения.
            /// </summary>
            internal int HashCode;
            /// <summary>
            /// Хранимое значением (сравниваем с ним только, если хеш-коды совпадают).
            /// </summary>
            internal T Value;
            /// <summary>
            /// Индекс следующего элемента цепочки в таблице. Признак окончания цепочки -1.
            /// </summary>
            internal int Next;
        }

        public MyHashSet()
        {
            _lastIndex = 0;     // начинаем вставлять с начала
            _count = 0;         // сейчас в хеш-таблице нет элементов
            _freeList = -1;     // список свободных элементов отсутствует.
        }

        /// <summary>
        /// Проверяет наличие элемента в хеш-таблице.
        /// </summary>
        /// <param name="item">Проверяемый элемент.</param>
        /// <returns>true, если содержится; в противном случае false.</returns>
        public bool Contains(T item)
        {
            if (_buckets == null) 
                return false; // корзины нет - элементов нет

            int hashCode = InternalGetHashCode(item); // неотрицательный

            // начинаем со своего слота, определяемого на основе корзины hashCode % _buckets.Length
            // в начальном состоянии _buckets[hashCode % _buckets.Length] равно 0, а значит i будет меньше 0.
            for (int i = _buckets[hashCode % _buckets.Length] - 1; i >= 0; i = _slots[i].Next)
            {
                // сравниваем содержимое слота 
                if (_slots[i].HashCode == hashCode && Equals(_slots[i].Value, item))
                    return true;

                // переходим дальше по цепочке слотов i = _slots[i].Next
            }

            return false; // дошли до конца цепочки
        }

        /// <summary>
        /// Добавляет заданный элемент.
        /// </summary>
        public void Add(T item)
        {
            AddIfNotPresent(item);
            this.Print();
        }

        /// <summary>
        /// Доабвляет диапазон элементов.
        /// </summary>
        public void AddRange(params T [] items)
        {
            foreach (var item in items)
                Add(item);
            
        }

        /// <summary>
        /// Добавляет элемент в хеш-таблицу, если его изначально не было.
        /// </summary>
        private bool AddIfNotPresent(T value)
        {
            if (_buckets == null)
                Initialize(0); // делаем хеш-таблицу из трех элементов

            // сначала проверяем есть ли уже этот элемент в хеш-таблице
            int hashCode = InternalGetHashCode(value);
            int position = hashCode % _buckets.Length;
            for (int i = _buckets[position] - 1; i >= 0; i = _slots[i].Next)
            {
                if (_slots[i].HashCode == hashCode && Equals(_slots[i].Value, value))
                    return false; // элемент уже есть в хеш-таблице
            }

            // элемента нет в хеш-таблице - определяем позицию для вставки
            int insertPosition;
            if (_freeList >= 0)
            {
                // есть цепочка удаленных элементов - будем вставлять вместо первого удаленного
                insertPosition = _freeList;
                _freeList = _slots[insertPosition].Next; // сокращаем список свободных элементов в цепочке
            }
            else
            {
                // цепочки удаленных нет
                if (_lastIndex == _slots.Length)
                {
                    // все слоты полностью заполнены - увеличиваем размер
                    IncreaseCapacity();
                    position = hashCode % _buckets.Length; // позицию надо расчитать заново, так как хеш-код нормируется текущим размером
                }
                insertPosition = _lastIndex; // вставляем по индексу последнего не занятого
                ++_lastIndex; // после вставки индекс увеличиваем на 1 - то есть физически заполняем элементы подряд
            }
            
            // делаем заполнение в позиции вставки
            _slots[insertPosition].HashCode = hashCode; 
            _slots[insertPosition].Value = value;

            // сложный момент - следующим делаем то, что раньше было началом цепочки в корзине, то есть 
            // фактически так организуется вставка в начало - следующим делаем то, что раньше было первым
            _slots[insertPosition].Next = _buckets[position] - 1;// денормализуем на 1 (так образуется -1 в конце цепочки)
            
            // сами становимся первыми
            _buckets[position] = insertPosition + 1; // искать после позиции вставки
            ++_count; // увеличиваем количество заполненных элементов

            return true; // элемент был добавлен
        }

        /// <summary>
        /// Инициализирует начальное заполнение хеш-таблицы.
        /// </summary>
        /// <param name="capacity">Ориентировочная начальная емкость хеш-таблицы.</param>
        private void Initialize(int capacity)
        {
            int prime = HashHelpers.GetPrime(capacity); // для capacity == 0 начальное количество - 3 элемента 
            _buckets = new int[prime]; // при начальном заполнении все нули, а это означает, что индексы всех слотов равны -1, так как увеличено на 1
            _slots = new Slot[prime];
        }

        /// <summary>
        /// Расширение хеш-таблицы при исчерпании свободных.
        /// </summary>
        private void IncreaseCapacity()
        {
            int min = _count * 2;
            if (min < 0)
                min = _count;

            int prime = HashHelpers.GetPrime(min); // ищем простое число большее чем в два раза больший размер
            if (prime <= _count) // съели все допустимые значения int - нереальная ситуация
                throw new ArgumentException(("Capacity Overflow"));

            // выделяем новый массив слотов
            Slot[] newSlots = new Slot[prime];
            if (_slots != null)
                Array.Copy(_slots, 0, newSlots, 0, _lastIndex); // копируем все заполненные значения _lastIndex равно предыдущему размеру

            int[] newBuckets = new int[prime]; // выделяем новый массив корзин

            // делаем перехеширование - ведь размер увеличился
            for (int i = 0; i < _lastIndex; ++i) // по всем заполненным элементам
            {
                // наш хеш-код не поменялся, зато поменялся наш индекс
                int newPosition = newSlots[i].HashCode % prime; // prime == newBuckets.Length, наша новая корзина

                newSlots[i].Next = newBuckets[newPosition] - 1; // делаем следующим, в списке то, что там было ранее первым
                newBuckets[newPosition] = i + 1; // сами становимся головой списка
            }

            _slots = newSlots;
            _buckets = newBuckets;
        }

        /// <summary>
        /// Возвращает хеш-код для указанного элемента.
        /// </summary>
        private int InternalGetHashCode(T item)
        {
            if (item == null)
                return 0;
            return item.GetHashCode() & int.MaxValue; // всегда положительный
        }

        /// <summary>
        /// Удаление заданного элемента из хеш-таблицы.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true, если было произведено удаления; false, если элемент отсутствует в хеш-таблице.</returns>
        public bool Remove(T item)
        {
            if (_buckets == null) // корзин нет, значит и дуалять нечего
                return false;

            int hashCode = InternalGetHashCode(item);
            int position = hashCode % _buckets.Length;
            int previousIndex = -1;

            // начинаем идти подряд цепочке
            for (int i = _buckets[position] - 1; i >= 0; i = _slots[i].Next)
            {
                if (_slots[i].HashCode == hashCode && Equals(_slots[i].Value, item))
                {
                    // элемент найден
                    if (previousIndex < 0)
                        _buckets[position] = _slots[i].Next + 1; // элемент идет первым в цепочке - меняем начало цепочки в _buckets
                    else
                        _slots[previousIndex].Next = _slots[i].Next; // элемент не идет первым - делаем его пропуск в цепочке

                    // зачищаем слот
                    _slots[i].HashCode = -1; // недопустимый хеш-код - ни с чем не совпадет при поиске, 
                                             // так как наши хеш-коды положительные (см. InternalGetHashCode)
                    _slots[i].Value = default(T); // пустое значение

                    // ставим слот в начало списка свободных
                    _slots[i].Next = _freeList;
                    _freeList = i; // теперь наш элемент в начале списка свободных
                    --_count; // уменьшаем число заполненных элементов
                    return true;
                }
                previousIndex = i; // сохраняем индекс предыдущего элемента
            }

            return false; // элемент не был найден
        }

        public void Print()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Buckets");

            sb.Append("| ");
            for (int i = 0; i < _buckets.Length; ++i)
                sb.Append(_buckets[i] + " | ");

            sb.AppendLine();
            sb.AppendLine("Slots");

            sb.AppendLine();
            sb.Append("| ");
            for (int i = 0; i < _slots.Length; ++i)
                sb.Append(_slots[i].HashCode + " | ");

            sb.AppendLine();
            sb.Append("| ");
            for (int i = 0; i < _slots.Length; ++i)
                sb.Append(_slots[i].Value + " | ");

            sb.AppendLine();
            sb.Append("| ");
            for (int i = 0; i < _slots.Length; ++i)
                sb.Append(_slots[i].Next + " | ");

            Trace.WriteLine(sb.ToString());
        }
    }
}