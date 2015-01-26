using System;
using System.Runtime.CompilerServices;
using NeedfulThings.EventStore;
using NeedfulThings.EventStore.MongoDB;
using NUnit.Framework;

[assembly: InternalsVisibleTo("NeedfulThings.EventStore")]

namespace EventStore.Tests
{
    [TestFixture]
    public class EventStoreFixture
    {
        [Test]
        public void Should_persist_aggregate_events()
        {
            var eventStore = new MongoEventStore();
            var repository = new EventStoreRepository<Account>(eventStore);

            var account = new Account();
            account.Deposit(100);
            account.Withdraw(75);
            repository.Save(account);

            account = repository.GetById(account.Id, id => new Account(id));

            Assert.AreEqual(25, account.Balance);

            var stream = eventStore.OpenStream(account.Id);
            Assert.AreEqual(account.Version, stream.Revision);
            Assert.AreEqual(1, stream.SeqNo);
        }

        [Test]
        public void Should_persist_aggregate_events_in_multiple_commits()
        {
            var eventStore = new MongoEventStore();
            var repository = new EventStoreRepository<Account>(eventStore);

            var account = new Account();
            repository.Save(account);
            
            account.Deposit(100);
            repository.Save(account);

            account.Withdraw(75);
            repository.Save(account);

            account = repository.GetById(account.Id, id => new Account(id));

            Assert.AreEqual(25, account.Balance);

            var stream = eventStore.OpenStream(account.Id);
            Assert.AreEqual(account.Version, stream.Revision);
            Assert.AreEqual(2, stream.SeqNo);
        }

        [Test]
        public void Throws_when_persisting_to_stream_with_same_or_greater_sequence()
        {
            var repository = new EventStoreRepository<Account>(new MongoEventStore());

            var account = new Account();
            account.Deposit(100);

            repository.Save(account);

            // Concurrent change
            var clone = repository.GetById(account.Id, id => new Account(id));
            clone.Withdraw(50);
            repository.Save(clone);

            account.Withdraw(75);
            Assert.Throws<ConcurrencyException>(() => repository.Save(account));
        }

        public class Account : AggregateRoot
        {
            public Account()
            {
            }

            public Account(Guid id) : base(id)
            {
            }

            public decimal Balance { get; private set; }

            public void Deposit(decimal amount)
            {
                ApplyEvent(new AccountBalanceChanged(amount), false);
            }

            public void Withdraw(decimal amount)
            {
                if (amount > Balance)
                    throw new InvalidOperationException("Not enough funds available.");

                ApplyEvent(new AccountBalanceChanged(-amount), false);
            }

            internal void ApplyEvent(AccountBalanceChanged @event)
            {
                Balance += @event.Amount;
            }

            public class AccountBalanceChanged : IEvent
            {
                public AccountBalanceChanged(decimal amount)
                {
                    Amount = amount;
                }

                public decimal Amount { get; private set; }
            }
        }
    }
}
