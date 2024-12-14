---
layout: post
title: "The Parseltongue Chronicles: Enchanting Embeddings with Python Decorators"
date: 2024-12-15
categories: [Python, Decorators, Machine Learning]
tags: [embeddings, python, decorators, optimization]
---

**Executive Summary**
This blog post delves into the complexities of embedding computation for structured data, illustrating how to streamline this process using Python decorators. By addressing common pitfalls such as excessive boilerplate and tightly coupled logic, we introduce a clean, modular approach using the concept of embedders. Through practical examples, we demonstrate how decorators can simplify embedding generation, reduce human error, and improve code maintainability. This solution is ideal for developers managing complex data transformations across a wide range of applications, not limited to Retrieval-Augmented Generation (RAG) systems.

## Enchanting Embeddings with Python Decorators

### Introduction

*What if you could instantly find the most relevant insights from a sea of data—like a librarian who knows exactly which book holds the answer to your question?*

Retrieval-Augmented Generation (RAG) solutions make this possible, but they rely on a critical step: the computation of embedding vectors.

In essence, embedding vectors are numerical representations of text data that allow us to measure relationships between terms in a mathematically precise way. By creating a vector database from a corpus of interesting terms, we can input a new term and ask the database which existing terms in the corpus are most related.

### Defining the BlogPost Class

Let’s start by considering a simple data class that might represent a unit of information in our corpus:

```python
@dataclass
class BlogPost:
    title: str
    summary: str
    topics: list[str]
    body: str
```
This class serves as a foundational structure for our database, encapsulating the key elements of a blog post while maintaining flexibility for future enhancements. We may find it interesting to be able to search our blog in the future by topic, by title, or by some term in the summary or body.

### Computing Embeddings
A function that computes embeddings for a given `BlogPost` may have this structure:

```python
def compute_embeddings(blog_post: BlogPost):
    title_vector = get_vector_from_somewhere(blog_post.title)
    summary_vector = get_vector_from_somewhere(blog_post.summary)
    # topic_vectors = [get_vector_from_somewhere(topic) for topic in blog_post.topics] # this is more complex, let's deal with this later!
    # body_vector = ...this is more complex, let's deal with this later...
```

### Storing Embeddings
Of course, we have to store these vectors somewhere, so we'll create a class for that:

```python
@dataclass
class BlogPostWithEmbeddings(BlogPost):
    title_vector: list[float]
    summary_vector: list[float]
```

### Avoiding Constructor Overload
Now, we could be tempted to naively stick the vector computation stuff inside the constructor of `BlogPostWithEmbeddings`, but that actually is part of the problem we need to tackle. This is because that `get_vector_from_somewhere` method actually isn't a simple method in reality. In reality, getting a vector happens through a chat client object, which comes with authentication information and all sorts of other contextual guff, which, traditionally, is in some sort of driver class where a whole bunch of concerns are conflated. Something like this:

```python
@dataclass
class BlogPostIndexer:
    ...
    chat_client_endpoint: str
    chat_client_apikey: secret
    ...

    def __post_init__(self):
        self.chat_client = build_chat_client(self.chat_client_endpoint, self.chat_client_apikey, ...)

    def vectorize(self, s: str) -> list[float]:
        return self.chat_client.compute_embedding(s)

    def compute_embeddings(self, blog_post: BlogPost):
        title_vector = self.vectorize(blog_post.title)
        summary_vector = self.vectorize(blog_post.summary)
```

### Challenges with This Approach
This may look clean and reasonable to the casual observer, and indeed, most code I have seen here looks like this, but this is actually problematic because you can't test the `compute_embeddings` function without some mocking.

Furthermore, imagine how this code evolves over time—the class with the source information may have dozens of fields. For each field to be indexed, we would need a corresponding field to store the embedding vector, and the embedding computation would need to be kept up to date. A simple change like adding a new indexable property would involve changes in three classes. Throw in cut-and-paste technology, and you might very quickly land up overwriting the wrong vector.

### To Summarize

* We want to maximally streamline the declaration that a given field should be indexed, and reduce or eliminate the possibility of misconfiguration.

## Step 1: Hiding Optics In Plain Sight
We're FP-oriented, so we obviously use the idea of [optics](https://medium.com/@gcanti/introduction-to-optics-lenses-and-prisms-3230e73bfcfe), right? Right?

(I'm joking—don't ever say this out loud in your team, or you risk being (not-so) gently ushered out of the team the moment you mention profunctors, lenses, and prisms. Wailing "But lenses compose..." is both a valid and ineffective argument. You have to be more subtle.)

### Goals for the Embedder

* We want an object—let's call it an `Embedder` — that can do two things:
  1. Focus on a given source property in an object and get its value.
  2. Focus on a given target property in an object and set its value to a given vector.
* We represent all the embedding computations as a list of `Embedder` objects.
* Given such a list of `Embedder` objects, and a vectorizing function, we can then write a simple function that takes a `BlogPost` object and returns a `BlogPostWithEmbeddings` object with each of the vector properties computed and set.


### Implementing the Embedder

```python
from typing import TypeVar, Generic
from collections.abc import Iterable, Callable

T = TypeVar(T)

@dataclass
class Embedder(Generic[T]):
    selector: Callable[[T], str]
    projector: Callable[[T, list[float]], T]

@dataclass
class EmbeddingGenerator(Generic[T]):
    embedders: list[Embedder[T]]
    ...

    def vectorize(self, key: str) -> list[float]:
        ...

    def compute_embedding(self, chunk: T) -> T:
        def compute_embedding_inner(chunk: T, embedder: Embedder[T]) -> T:
            key = embedder.selector(chunk)
            try:
                vector = self.vectorize(key)
                result = embedder.projector(chunk, vector)
                if result is None:
                    raise ValueError("CRITICAL ERROR. Projector returned None.")
                return result
            except Exception as e:
                print(f"Failed to get embedding for key '{key}': {e}")
                return chunk

        return reduce(compute_embedding_inner, self.embedders, chunk)
```

### Writing Embedders for Each Use Case

```python
@dataclass
class BlogPostWithEmbeddings:
    title: str
    summary: str
    topics: list[str]
    body: str
    title_vector: list[float] = field(default_factory=list)
    summary_vector: list[float] = field(default_factory=list)
    topic_vectors: list[list[float]] = field(default_factory=list)

title_embedder = Embedder[BlogPostWithEmbeddings](
    selector=lambda o: o.title,
    projector=lambda o, v: (o.__setattr__('title_vector', v), o)[1]
)

summary_embedder = Embedder[BlogPostWithEmbeddings](
    selector=lambda o: o.summary,
    projector=lambda o, v: (o.__setattr__('summary_vector', v), o)[1]
)
```

### Putting It All Together

```python
embedding_generator = EmbeddingGenerator[BlogPostWithEmbeddings](embedders=[title_embedder, summary_embedder, ...])
```

And compute the embeddings this way:

```python
blog_post_with_embeddings = embedding_generator.compute_embedding(blog_post)
```

## Stage 2: Poof! Making The Boilerplate Vanish

No, we're not done yet!

We still have to manually write those embedders, and we've actually introduced more boilerplate now, which is more annoying. But we have managed to separate out the concern of defining the embeddings from the concern of applying the embeddings on an object, so at least that is a win.

Now let's tackle the problem of eliminating the boilerplate.

### Speaking The Spell in Native Parseltongue

Enter a really cool idiom in Python - the `decorator` object.

A `decorator` is basically magic, augmenting and transforming the thing you are decorating in a way that hides boilerplate.

Let's review what our boilerplate for each embeddable property is:

A field to store each embedding vector, always of type `list[float]`

An `Embedder` object properly constructed to associate the source field and the target embedding field mentioned above

A nice way to keep track of all the `Embedder` objects associated with a class so we can neatly create an `EmbeddingGenerator` object

What we want is a decorator to automatically inject these things into our BlogPost object so we don't have to worry about hand-writing any of these!

Cool? Cool! Let's write such an embedder:

```python
def add_embedder[T](
        source_property_name: str,
        destination_property_name: str | None = None,
        embedder_name: str | None = None) -> Embedder[T]:
    def decorator(cls: T) -> T:
        """Inject fields for an embedding and an embedder object."""
        gen_destination_property_name: str = destination_property_name or f"{source_property_name}_vector"
        gen_embedder_name: str = embedder_name or f"{source_property_name}_{destination_property_name}embedding"
        setattr(cls, gen_destination_property_name, field(default_factory=list))

        def selector(x: T) -> str:  # The source property value needs to be a string
            return getattr(x, source_property_name)

        def projector(x: T, v: list[float]) -> T:   # ensure that the projector returns the modified object
            return (setattr(x, gen_destination_property_name, v), x)[1]

        embedder = Embedder[T](selector=selector, projector=projector)

        setattr(cls, gen_embedder_name, embedder)

        # add this embedder to the existing list of embedders
        existing_embedders = getattr(cls, "embedders", [])
        setattr(cls, "embedders", existing_embedders + [embedder])
        return cls

    return decorator
```

This little magic spell lifts all the boilerplate of defining the destination `vector_field`, defining the `embedder` with its `selector` and `projector`, and keeping track of all the embedders in an `embedders` field on the class, so our final use case looks like this:

```python
@add_embedder(title)
@add_embedder(summary)
@dataclass
class BlogPost:
    title: str
    summary: str
    topics: list[str]
    body: str

...

embedding_generator = EmbeddingGenerator[BlogPost](embedders=BlogPost.embedders)

...

blog_post_with_embeddings = embedding_generator.compute_embedding(blog_post)
print(blog_post_with_embeddings.title_vector)  # this was created and injected by the decorator...
```

And now, we're done. No boilerplate, or even any real user-references to the `Embedder` class!

We can put our wands down now!

### Conclusion

By using decorators and embedders, we effectively streamline the process of embedding computation while reducing boilerplate code and minimizing human error. This approach also allows us to separate concerns cleanly, making the codebase easier to maintain and extend. With these tools, handling complex data representations becomes more intuitive and efficient, empowering developers to focus on solving the real problems in their applications.
