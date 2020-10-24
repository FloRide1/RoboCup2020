using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utilities
{
    /// <summary>Met à disposition quelques extensions de classes courantes.</summary>
    public static class Extensions
    {
        /// <summary>
        /// Permet d'obtenir le tableau d'octets correspondant à cette chaîne de caractères.
        /// </summary>
        /// <param name="s"><see cref="string"/> à traduire.</param>
        public static byte[] GetBytes(this string s) => Encoding.ASCII.GetBytes(s);

        /// <summary>
        /// Permet d'obtenir le <see cref="string"/> correspondant au tableau d'octets rentré.
        /// </summary>
        /// <param name="bs">Array d'octets à traduire.</param>
        public static string GetString(this byte[] bs) => Encoding.ASCII.GetString(bs);

        /// <summary>
        /// Permet d'obtenir le tableau d'octets correspondant à ce float.
        /// </summary>
        /// <param name="f"><see cref="float"/> à traduire.</param>
        public static unsafe byte[] GetBytes(this float f)
        {
            byte* f_ptr = (byte*)&f;

            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
                bytes[i] = f_ptr[i];

            return bytes;
        }

        /// <summary>
        /// Permet d'obtenir le tableau d'octets correspondant à ce double.
        /// </summary>
        /// <param name="d"><see cref="double"/> à traduire.</param>
        public static unsafe byte[] GetBytes(this double d)
        {
            byte* d_ptr = (byte*)&d;

            byte[] bytes = new byte[8];
            for (int i = 0; i < 8; i++)
                bytes[i] = d_ptr[i];

            return bytes;
        }

        /// <summary>
        /// Permet d'obtenir le tableau d'octets correspondant à ce INT32.
        /// </summary>
        /// <param name="d"><see cref="double"/> à traduire.</param>
        public static unsafe byte[] GetBytes(this Int32 i)
        {
            byte[] bytes = new byte[4];
            bytes[0] = (byte)(i >> 24);
            bytes[1] = (byte)(i >> 16);
            bytes[2] = (byte)(i >> 8);
            bytes[3] = (byte)i;
            return bytes;
        }
        /// <summary>
        /// Permet d'obtenir le <see cref="float"/> correspondant au tableau d'octets rentré.
        /// </summary>
        /// <param name="b">Array d'octets à traduire.</param>
        public static unsafe float GetFloat(this byte[] b)
        {
            float f;
            fixed (byte* b_ptr = b)
                f = *(float*)b_ptr;

            return f;
        }

        /// <summary>
        /// Permet d'obtenir le <see cref="double"/> correspondant au tableau d'octets rentré.
        /// </summary>
        /// <param name="b">Array d'octets à traduire.</param>
        public static unsafe double GetDouble(this byte[] b)
        {
            double d;
            fixed (byte* b_ptr = b)
                d = *(double*)b_ptr;

            return d;
        }

        /// <summary>
        /// Remplit une zone de cet array avec des données spécifiées.
        /// </summary>
        /// <param name="b">Array auquel ajouter des données.</param>
        /// <param name="dataToInsert">Données à insérer.</param>
        /// <param name="index">Index de cette instance de byte[] à partir duquel insérer les données.</param>
        /// <param name="length">Nombre de données à insérer.</param>
        public static void SetValueRange<T>(this T[] b, T[] dataToInsert, int index, int length = 4)
        {
            for (int i = 0; i < length; i++)
                b[index + i] = dataToInsert[i];
        }

        /// <summary>
        /// Permet d'obtenir une copie d'une Range de l'array spécifié.
        /// </summary>
        /// <param name="b">Array source de données à copier.</param>
        /// <param name="index">Index à partir duquel copier.</param>
        /// <param name="length">Nombre de données à copier.</param>
        public static T[] GetRange<T>(this T[] b, int index, int length = 4)
        {
            T[] b_final = new T[length];
            Array.Copy(b, index, b_final, 0, length);

            return b_final;
        }

        /// <summary>
        /// Permet d'ajouter une paire clé/valeur à un dictionnaire ou de la mettre à jour si déjà existante.
        /// </summary>
        /// <param name="d">Dictionnaire à manipuler.</param>
        /// <param name="key">Clé à ajouter/mettre à jour.</param>
        /// <param name="value">Valeur à rentrer.</param>
        public static void AddOrUpdate<T, T2>(this Dictionary<T, T2> d, T key, T2 value)
        {
            if (d.ContainsKey(key))
                d[key] = value;
            else
                d.Add(key, value);
        }
    }

    public class RollingList<T> : List<T>
    {
        private readonly LinkedList<T> _list = new LinkedList<T>();

        public RollingList(int maximumCount)
        {
            if (maximumCount <= 0)
                throw new ArgumentException(null, nameof(maximumCount));

            MaximumCount = maximumCount;
        }

        public int MaximumCount { get; }
        new public int Count => _list.Count;

        new public void Add(T value)
        {
            if (_list.Count == MaximumCount)
            {
                _list.RemoveFirst();
            }
            _list.AddLast(value);
        }

        new public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException();

                return _list.Skip(index).First();
            }
        }

        new public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
    }
}
