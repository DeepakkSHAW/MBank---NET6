using MBank.API.Models;

namespace MBank.API.Services
{
    public class DummyCustomerRepository
    {
        private readonly Dictionary<Guid, DummyCustomer> _customer = new();
        public List<DummyCustomer> GetAll()
        {
            return _customer.Values.ToList();
        }
        public DummyCustomer GetById(Guid id)
        {
            return _customer[id];
        }
        public void Create(DummyCustomer customer)
        {
            if (customer == null)
                return;
            _customer.Add(customer.Id, customer);
        }
        public void Delete(Guid id)
        {
            _customer.Remove(id);
        }
        public void Update(DummyCustomer customer)
        {
            var exisitingCustomer = _customer[customer.Id];
            if (exisitingCustomer == null)
                return;
            _customer[customer.Id] = customer;
        }
    }
}
