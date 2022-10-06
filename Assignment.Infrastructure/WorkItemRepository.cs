namespace Assignment.Infrastructure;
using Assignment.Core;

public class WorkItemRepository : IWorkItemRepository
{

    private readonly KanbanContext _context;

    public WorkItemRepository(KanbanContext context)
    {
        _context = context;
    }

    public (Response Response, int ItemId) Create(WorkItemCreateDTO workItem) 
    {
        var entity = _context.Items.FirstOrDefault(c => c.Title == workItem.Title);
        Response response;

        if (entity is null)
        {
            var assignedToTemp = _context.Users.FirstOrDefault(c => c.Id == workItem.AssignedToId.Value);
            
            if (assignedToTemp is null && workItem.AssignedToId.Value is not 0){
                return (Response.NotFound, 0);
            } 
            
            else{
                ICollection<Tag> tagsTemp = new List<Tag>();
                if (workItem.Tags is not null && _context.Tags is not null){
                    tagsTemp = _context.Tags.Where(t => workItem.Tags.Any(x => x == t.Name)).ToArray();
                }
                entity = new WorkItem(workItem.Title){AssignedTo = assignedToTemp, Tags = tagsTemp, State = State.New};

                _context.Items.Add(entity);
                _context.SaveChanges();

                response = Response.Created;
            }
        }
        else
        {
            response = Response.Conflict;
        }
        return (response, entity.Id);
    }
    public IReadOnlyCollection<WorkItemDTO> Read()
    {
        var workItems = from c in _context.Items
        orderby c.Title
        select new WorkItemDTO(c.Id, c.Title, c.AssignedTo.Name, c.Tags.Select(c => c.Name).ToArray(), c.State);

        return workItems.ToArray();
    }
    public IReadOnlyCollection<WorkItemDTO> ReadRemoved() 
    {
        var removedWorkItems = from c in _context.Items
        where c.State == State.Removed
        orderby c.Title
        select new WorkItemDTO(c.Id, c.Title, c.AssignedTo.Name, c.Tags.Select(c => c.Name).ToArray(), c.State);

        return removedWorkItems.ToArray();
    }
    public IReadOnlyCollection<WorkItemDTO> ReadByTag(string tag) 
    {
        var tagWorkItems = from c in _context.Items
        where c.Tags.Any(x => x.Name == tag)
        orderby c.Title
        select new WorkItemDTO(c.Id, c.Title, c.AssignedTo.Name, c.Tags.Select(c => c.Name).ToArray(), c.State);

        return tagWorkItems.ToArray();
    }
    public IReadOnlyCollection<WorkItemDTO> ReadByUser(int userId)
    {
        var userWorkItems = from c in _context.Items
        where c.AssignedTo.Id == userId
        orderby c.Title
        select new WorkItemDTO(c.Id, c.Title, c.AssignedTo.Name, c.Tags.Select(c => c.Name).ToArray(), c.State);

        return userWorkItems.ToArray();
    }
    public IReadOnlyCollection<WorkItemDTO> ReadByState(State state)
    {
        var tagWorkItems = from c in _context.Items
        where c.State == state
        orderby c.Title
        select new WorkItemDTO(c.Id, c.Title, c.AssignedTo.Name, c.Tags.Select(c => c.Name).ToArray(), c.State);

        return tagWorkItems.ToArray();
    }
    public WorkItemDetailsDTO Find(int workItemId)
    {
        var workItemDetails = from c in _context.Items
        where c.Id == workItemId 
        select new WorkItemDetailsDTO{Id = c.Id, Title = c.Title, AssignedToName = c.AssignedTo.Name, Tags = c.Tags.Select(c => c.Name).ToArray(), State = c.State};
        return workItemDetails.FirstOrDefault();
    }
    public Response Update(WorkItemUpdateDTO WorkItem) 
    {
       var entity = _context.Items.Find(WorkItem.Id);
       Response response;
        if (entity is null)
        {
            response = Response.NotFound;
        }
         else if (_context.Items.FirstOrDefault(w => w.Id != WorkItem.Id && w.Title == WorkItem.Title) != null)
        {
            response = Response.Conflict;
        }
        else
        {
            entity.Title = WorkItem.Title;
            if (WorkItem.AssignedToId is not null)
            {
                var temp = _context.Users.Where(c => c.Id == WorkItem.AssignedToId.Value).FirstOrDefault();
                if (temp is null) {response = Response.BadRequest;}
                else {entity.AssignedTo = temp;}
            }

            entity.Tags = _context.Tags.Where(t => WorkItem.Tags.Any(x => x == t.Name)).ToArray();

            if (entity.State != WorkItem.State){
                entity.State = WorkItem.State;
            }
                
            _context.SaveChanges();
            response = Response.Updated;
        }
        return response;
    }

    public Response Delete(int workItemId)
    {
        var entity = _context.Items.Find(workItemId);
       Response response;
        if (entity is null)
        {
            response = Response.NotFound;
        }
        else{
            switch (entity.State)
            {
            case State.Removed: case State.Closed: case State.Resolved:
                response = Response.Conflict;
            break;

            case State.Active:
                entity.State = State.Removed;
                _context.SaveChanges();
                response = Response.Updated;
            break;

            case State.New:
                _context.Items.Remove(entity);
                _context.SaveChanges();
                response = Response.Deleted;
            break;

            default:
                response = Response.BadRequest;
                break;
            }
        }
         
        return response;
    }
    

}

