using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;

namespace MTS_plugin.Database
{
    public class LocalStorage : IDisposable
    {
        private readonly Lazy<LiteDatabase> _lazyDatabase;

        private LiteDatabase Db => _lazyDatabase.Value;

        public LocalStorage(string path)
        {
            _lazyDatabase = new Lazy<LiteDatabase>(() =>
            {
                var db = new LiteDatabase(Path.Combine(path, @"MTS-plugin.db"));
                return db;
            }, true);
        }

        public void AddRange<T>(IEnumerable<T> items)
        {
            Db.GetCollection<T>().InsertBulk(items);
        }

        public void Add<T>(T entity)
        {
            if (entity == null)
                return;

            Db.GetCollection<T>().Insert(entity);
        }

        public IEnumerable<T> Get<T>(Expression<Func<T, bool>> predicate = null)
        {
            var collection = Db.GetCollection<T>();
            if (predicate == null)
                return collection.FindAll();

            return collection.Find(predicate);
        }

        public void Dispose()
        {
            if (!_lazyDatabase.IsValueCreated)
                return;

            _lazyDatabase.Value?.Dispose();
        }

        public bool Upsert<T>(T entity)
        {
            return Db.GetCollection<T>().Upsert(entity);
        }

        public int Delete<T>(Expression<Func<T, bool>> predicate)
        {
            return Db.GetCollection<T>().Delete(predicate);
        }

        public bool Delete<T>(Guid key)
        {
            return Db.GetCollection<T>().Delete(key);
        }
        public bool Delete<T>(long key)
        {
            return Db.GetCollection<T>().Delete(key);
        }
        public bool Delete<T>(string key)
        {
            return Db.GetCollection<T>().Delete(key);
        }

        public bool Any<T>(Expression<Func<T, bool>> predicate)
        {
            return Db.GetCollection<T>().Exists(predicate);
        }
    }
}
