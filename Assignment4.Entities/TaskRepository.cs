using System.Collections.Generic;
using System.Collections.Immutable;
using Assignment4.Core;
using System.Linq;
using System;

namespace Assignment4.Entities
{
    public class TaskRepository : ITaskRepository
    {
        private KanbanContext _kanbanContext;

        public TaskRepository(KanbanContext kanbanContext)
        {
            _kanbanContext = kanbanContext;
        }

        public (Response Response, int TaskId) Create(TaskCreateDTO task)
        {
            if (GetUser(task.AssignedToId) == null) return (Response.BadRequest, 0);

            var entity = new Task
            {
                Title = task.Title,
                AssignedTo = GetUser(task.AssignedToId),
                Description = task.Description,
                Created = DateTime.UtcNow,
                State = State.New,
                Tags = GetTags(task.Tags).ToList(),
                StateUpdated = DateTime.UtcNow
            };

            _kanbanContext.Tasks.Add(entity);
            _kanbanContext.SaveChanges();

            return (Response.Created, entity.Id);
        }

        public Response Delete(int taskId)
        {
            var task = _kanbanContext.Tasks.FirstOrDefault(x => x.Id == taskId);
            if (task == null) return Response.NotFound;
            if (task.State == State.Resolved || task.State == State.Closed || task.State == State.Removed) return Response.Conflict;
            if (task.State == State.Active) task.State = State.Removed;

            if (task.State == State.New)
            {
                _kanbanContext.Tasks.Remove(task);
            }

            _kanbanContext.SaveChanges();

            return Response.Deleted;
        }

        public TaskDetailsDTO Read(int taskId)
        {
            var task = _kanbanContext.Tasks.FirstOrDefault(x => x.Id.Equals(taskId));
            return task == null ? null : new TaskDetailsDTO(task.Id, task.Title, task.Description, task.Created, task.AssignedTo.Name, task.Tags.Select(x => x.Name).ToImmutableList(), task.State, task.StateUpdated);
        }

        public IReadOnlyCollection<TaskDTO> ReadAll()
        {
            return _kanbanContext.Tasks.Select<Task, TaskDTO>(x => new TaskDTO(
                x.Id,
                x.Title,
                x.AssignedTo.Name,
                x.Tags.Select(y => y.Name).ToImmutableList<string>(),
                x.State
            )).ToList().AsReadOnly();
        }

        public IReadOnlyCollection<TaskDTO> ReadAllByState(State state)
        {
            return _kanbanContext.Tasks.Where(x => x.State == state).Select<Task, TaskDTO>(x => new TaskDTO(
                x.Id,
                x.Title,
                x.AssignedTo.Name,
                x.Tags.Select(y => y.Name).ToImmutableList<string>(),
                x.State
            )).ToImmutableList<TaskDTO>();
        }

        public IReadOnlyCollection<TaskDTO> ReadAllByTag(string tag)
        {
            return _kanbanContext.Tasks.Where(x => x.Tags.Select(y => y.Name).Contains(tag)).Select<Task, TaskDTO>(x => new TaskDTO(
                x.Id,
                x.Title,
                x.AssignedTo.Name,
                x.Tags.Select(y => y.Name).ToImmutableList<string>(),
                x.State
            )).ToImmutableList<TaskDTO>();
        }

        public IReadOnlyCollection<TaskDTO> ReadAllByUser(int userId)
        {
            return _kanbanContext.Tasks.Where(x => x.AssignedTo.Id == userId).Select<Task, TaskDTO>(x => new TaskDTO(
                x.Id,
                x.Title,
                x.AssignedTo.Name,
                x.Tags.Select(y => y.Name).ToImmutableList<string>(),
                x.State
            )).ToImmutableList<TaskDTO>();
        }

        public IReadOnlyCollection<TaskDTO> ReadAllRemoved()
        {
            return _kanbanContext.Tasks.Where(x => x.State == State.Removed).Select<Task, TaskDTO>(x => new TaskDTO(
                x.Id,
                x.Title,
                x.AssignedTo.Name,
                x.Tags.Select(y => y.Name).ToImmutableList<string>(),
                x.State
            )).ToImmutableList<TaskDTO>();
        }

        public Response Update(TaskUpdateDTO task)
        {
            var t = _kanbanContext.Tasks.FirstOrDefault(x => x.Id == task.Id);
            var user = GetUser(task.AssignedToId);

            if (t == null) return Response.NotFound;
            if (user == null) return Response.BadRequest;

            t.Title = task.Title;

            t.AssignedTo = user;

            t.Description = task.Description;

            if (t.State != task.State)
            {
                t.State = task.State;
                t.StateUpdated = DateTime.UtcNow;
            }

            t.Tags = GetTags(task.Tags).ToList();

            _kanbanContext.SaveChanges();

            return Response.Updated;
        }

        private IEnumerable<Tag> GetTags(IEnumerable<string> tags)
        {
            var existing = _kanbanContext.Tags.Where(x => tags.Contains(x.Name)).ToDictionary(x => x.Name);

            foreach (var tag in tags)
            {
                yield return existing.TryGetValue(tag, out var x) ? x : new Tag { Name = tag };
            }
        }

        private User GetUser(int? id)
        {
            return _kanbanContext.Users.FirstOrDefault(u => u.Id == id);
        }
    }
}
