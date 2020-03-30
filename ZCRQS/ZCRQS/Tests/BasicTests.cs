using System;
using System.Linq;
using Program.Aggregates;
using Program.Commands;
using Program.Entities;
using Program.Events;
using Program.Handlers;
using Xunit;

namespace Program.Tests
{
    public class BasicTests
    {
        [Fact]
        public void ShouldCreateNewAggregate()
        {
            var appendOnly = new MemoryAppendOnlyStore();
            var eventStore = new EventStore(appendOnly);
            var rootId = Guid.NewGuid();
            var root = ProductCatalogAggregate.CreateRoot(rootId);

            Assert.Equal(1, root.Changes.Count);
            Assert.Equal(rootId,root.Id);
        }
        
        [Fact]
        public void ShouldSaveStream()
        {
            var rootId = Guid.NewGuid();
            var appendOnly = new MemoryAppendOnlyStore();
            var eventStore = new EventStore(appendOnly);
            var root = ProductCatalogAggregate.CreateRoot(rootId);
            
            eventStore.AppendToStream(root.Id, 0, root.Changes);

            var stream = eventStore.LoadEventStream(rootId);
            Assert.Equal(1, stream.Version);
            Assert.Equal(typeof(ProductCatalogAggregateCreated), stream.Events.ElementAt(0).GetType());
            
            
        }
        
        [Fact]
        public void ShouldAggregateLoadStream()
        {
            var rootId = Guid.NewGuid();
            var appendOnly = new MemoryAppendOnlyStore();
            var eventStore = new EventStore(appendOnly);
            var rootToSave = ProductCatalogAggregate.CreateRoot(rootId);
            
            eventStore.AppendToStream(rootToSave.Id, 1, rootToSave.Changes);

            var stream = eventStore.LoadEventStream(rootId);
            var root = new ProductCatalogAggregate(stream.Events);
            
            Assert.True(0 == root.Changes.Count);
            Assert.Equal(rootId, root.Id);
        }
        
        //TODO: esse teste está sem objetivo claro
        [Fact]
        public void ShouldAllEventsRegisteredAggregate()
        {
            var root = ProductCatalogAggregate.CreateRoot(Guid.NewGuid());

            Assert.Equal(1, root.Changes.Count);
            Assert.Equal(typeof(ProductCatalogAggregateCreated), root.Changes.ElementAt(0).GetType());
        }
        
        //TODO: eu não lembro se apaguei os asserts?
        [Fact]
        public void EventsAppendedToChanges()
        {
            var appendOnly = new MemoryAppendOnlyStore();
            var eventStore = new EventStore(appendOnly);
            var productService = new ProductServiceCommandHandler(eventStore);
            var customerService = new CustomerService(eventStore);
            var command = new OrderServiceCommandHandler(eventStore, productService, customerService);
            
            
            command.Execute(new AddProductCommand(Guid.NewGuid(), Guid.NewGuid(), 2));
        }
        
        [Fact]
        public void ShouldAddProductToProductCatalog()
        {
            var rootId = Guid.NewGuid();
            var appendOnly = new MemoryAppendOnlyStore();
            var eventStore = new EventStore(appendOnly);
            var rootToSave = ProductCatalogAggregate.CreateRoot(rootId);
            
            eventStore.AppendToStream(rootToSave.Id, 1, rootToSave.Changes);

            var stream = eventStore.LoadEventStream(rootId);
            var root = new ProductCatalogAggregate(stream.Events);
            
            root.CreateProduct(new CreateProductCommand(Guid.NewGuid(), "Notebook", "Dell Inspiron 15000"));
            
            Assert.True(root.Changes.Count == 1);
            Assert.True(root.Changes.ElementAt(0).GetType() == typeof(ProductCreated));
        }
        
        [Fact]
        public void ShouldIncrementVersionCorrectly()
        {
            var rootId = Guid.NewGuid();
            var appendOnly = new MemoryAppendOnlyStore();
            var eventStore = new EventStore(appendOnly);
            var rootToSave = ProductCatalogAggregate.CreateRoot(rootId);
            
            eventStore.AppendToStream(rootToSave.Id, 1, rootToSave.Changes);

            var stream = eventStore.LoadEventStream(rootId);
            var root = new ProductCatalogAggregate(stream.Events);
            
            root.CreateProduct(new CreateProductCommand(Guid.NewGuid(), "Notebook", "Dell Inspiron 15000"));
            
            eventStore.AppendToStream(rootToSave.Id, 1, root.Changes);

            stream = eventStore.LoadEventStream(rootId);
            root = new ProductCatalogAggregate(stream.Events);
            
            Assert.Equal(2, stream.Version);
            Assert.Equal(1, root.CountProducts());
        }
    }
}