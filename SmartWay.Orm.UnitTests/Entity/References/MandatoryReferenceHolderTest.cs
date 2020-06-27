﻿using Moq;
using NUnit.Framework;
using SmartWay.Orm.Entity;
using SmartWay.Orm.Entity.References;
using SmartWay.Orm.Repositories;
using SmartWay.Orm.Testkit.Entities;

namespace SmartWay.Orm.UnitTests.Entity.References
{
    [TestFixture]
    public class MandatoryReferenceHolderTest
    {
        private static void CheckIsConsistant(IDistinctableEntity book, MandatoryReferenceHolder<Book> manadatory)
        {
            Assert.AreEqual(book, manadatory.Object);
            Assert.AreEqual(book.Id, manadatory.Id);
        }

        [Test]
        public void WhenFirstSetById_ReferenceShouldBeConsistant()
        {
            var book = new Book {Id = 10};
            var repo = new Mock<IRepository<Book>>();
            repo.Setup(_ => _.GetById(book.Id)).Returns(book);
            var manadatory = new MandatoryReferenceHolder<Book>(repo.Object) {Id = book.Id};


            CheckIsConsistant(book, manadatory);
        }

        [Test]
        public void WhenFirstSetByObject_ReferenceShouldBeConsistant()
        {
            var book = new Book {Id = 10};
            var repo = new Mock<IRepository<Book>>();
            repo.Setup(_ => _.GetById(book.Id)).Returns(book);
            var manadatory = new MandatoryReferenceHolder<Book>(repo.Object) {Object = book};


            CheckIsConsistant(book, manadatory);
        }

        [Test]
        public void WhenObjectNotPersistedOnInit_ReferenceShouldReturn()
        {
            var bookRepo = new Mock<IRepository<Book>>();
            var bookVersionRepo = new Mock<IRepository<BookVersion>>();
            var book = new Book(bookVersionRepo.Object);
            var bookversion = new BookVersion(bookRepo.Object) {Book = book};

            book.Id = 10;

            Assert.AreEqual(10, bookversion.BookId);
        }

        [Test]
        public void WhenSetFirstByIdThenByObject_ReferenceShouldBeConsistant()
        {
            var book = new Book {Id = 10};
            var book2 = new Book {Id = 20};
            var repo = new Mock<IRepository<Book>>();
            repo.Setup(_ => _.GetById(book.Id)).Returns(book);
            repo.Setup(_ => _.GetById(book2.Id)).Returns(book2);
            var manadatory = new MandatoryReferenceHolder<Book>(repo.Object) {Id = book.Id, Object = book2};


            CheckIsConsistant(book2, manadatory);
        }

        [Test]
        public void WhenSetFirstByObjectThenById_ReferenceShouldBeConsistant()
        {
            var book = new Book {Id = 10};
            var book2 = new Book {Id = 20};
            var repo = new Mock<IRepository<Book>>();
            repo.Setup(_ => _.GetById(book.Id)).Returns(book);
            repo.Setup(_ => _.GetById(book2.Id)).Returns(book2);
            var manadatory = new MandatoryReferenceHolder<Book>(repo.Object) {Object = book, Id = book2.Id};


            CheckIsConsistant(book2, manadatory);
        }
    }
}