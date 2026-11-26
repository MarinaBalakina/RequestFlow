create table if not exists users (
    id int generated always as identity primary key,
    login varchar(100) not null unique,
    password_hash varchar(100) not null,
    password_salt varchar(100) not null,
    name varchar(100) not null,
    surname varchar(100) not null,
    age int not null check (age between 0 and 150)
);
